using RepositoryLayer.Entities;
using RepositoryLayer.Enums;
using RepositoryLayer.Interfaces;
using ServiceLayer.Contracts.Inventory;

namespace ServiceLayer.Services.InventoryManagement;

public class PreOrderAvailabilityReconciliationService(
    IUnitOfWork unitOfWork) : IPreOrderAvailabilityReconciliationService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task ReconcileAfterStockIncreaseAsync(
        int variantId,
        CancellationToken cancellationToken = default)
    {
        var inventoryRepository = _unitOfWork.Repository<Inventory>();
        var orderRepository = _unitOfWork.Repository<Order>();

        var inventorySnapshot = await inventoryRepository.GetFirstOrDefaultAsync(
            inventory => inventory.VariantId == variantId,
            tracked: false);

        if (inventorySnapshot is null
            || !inventorySnapshot.IsPreOrderAllowed
            || inventorySnapshot.Quantity <= 0)
        {
            return;
        }

        var awaitingPreOrders = await orderRepository.FindAsync(
            filter: order =>
                order.OrderType == OrderType.PreOrder
                && order.OrderStatus == OrderStatus.AwaitingStock
                && order.OrderItems.Any(orderItem => orderItem.VariantId == variantId),
            includeProperties: "OrderItems",
            tracked: false);

        var waitingPreOrderDemand = awaitingPreOrders.Sum(order =>
            order.OrderItems
                .Where(orderItem => orderItem.VariantId == variantId)
                .Sum(orderItem => orderItem.Quantity));

        if (inventorySnapshot.Quantity <= waitingPreOrderDemand)
        {
            return;
        }

        var trackedInventory = await inventoryRepository.GetFirstOrDefaultAsync(
            inventory => inventory.VariantId == variantId,
            tracked: true);

        if (trackedInventory is null || !trackedInventory.IsPreOrderAllowed)
        {
            return;
        }

        trackedInventory.IsPreOrderAllowed = false;
        trackedInventory.ExpectedRestockDate = null;
        trackedInventory.PreOrderNote = null;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
