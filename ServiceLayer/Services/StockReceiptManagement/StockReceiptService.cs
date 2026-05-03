using RepositoryLayer.Common;
using RepositoryLayer.Entities;
using RepositoryLayer.Enums;
using RepositoryLayer.Interfaces;
using ServiceLayer.Contracts.Notifications;
using ServiceLayer.Contracts.StockReceipt;
using ServiceLayer.DTOs.StockReceipt.Request;
using ServiceLayer.DTOs.StockReceipt.Response;

namespace ServiceLayer.Services.StockReceiptManagement;

public class StockReceiptService(
    IUnitOfWork unitOfWork,
    IPreOrderBackInStockNotificationService backInStockNotificationService) : IStockReceiptService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IPreOrderBackInStockNotificationService _backInStockNotificationService = backInStockNotificationService;

    public async Task<StockReceiptDtoResponse> CreateStockReceiptAsync(
        CreateStockReceiptRequest request,
        int staffUserId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var variantRepository = _unitOfWork.Repository<ProductVariant>();
        var inventoryRepository = _unitOfWork.Repository<Inventory>();
        var receiptRepository = _unitOfWork.Repository<RepositoryLayer.Entities.StockReceipt>();
        var now = DateTime.UtcNow;
        var previousQuantity = 0;
        var currentQuantity = 0;
        RepositoryLayer.Entities.StockReceipt? stockReceipt = null;

        try
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            var variant = await variantRepository.GetFirstOrDefaultAsync(
                variant => variant.VariantId == request.VariantId,
                includeProperties: "Inventory",
                tracked: true);

            if (variant is null)
            {
                throw new KeyNotFoundException($"ProductVariant with id {request.VariantId} not found.");
            }

            var inventory = variant.Inventory;

            if (inventory is null)
            {
                inventory = new Inventory
                {
                    VariantId = variant.VariantId,
                    Quantity = 0,
                    IsPreOrderAllowed = false
                };

                await inventoryRepository.AddAsync(inventory);
                variant.Inventory = inventory;
            }

            // Inventory rule: stock receipt is the authoritative source for increasing on-hand quantity.
            previousQuantity = inventory.Quantity;
            inventory.Quantity += request.QuantityReceived;
            currentQuantity = inventory.Quantity;

            stockReceipt = new RepositoryLayer.Entities.StockReceipt
            {
                VariantId = request.VariantId,
                QuantityReceived = request.QuantityReceived,
                ReceivedDate = now,
                StaffId = staffUserId,
                Note = request.Note?.Trim()
            };

            await receiptRepository.AddAsync(stockReceipt);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }

        await _backInStockNotificationService.HandleStockChangeAsync(
            request.VariantId,
            previousQuantity,
            currentQuantity,
            // Demo note: this source unlocks stock-restoration transition context for pre-orders.
            source: "stock-receipt:create",
            cancellationToken);

        await ReconcilePreOrderAvailabilityAfterStockReceiptAsync(request.VariantId, cancellationToken);

        var createdReceipt = await receiptRepository.GetFirstOrDefaultAsync(
            receipt => receipt.ReceiptId == stockReceipt!.ReceiptId,
            includeProperties: "Staff",
            tracked: false);

        return MapToDto(createdReceipt ?? stockReceipt!);
    }

    public async Task<PagedResult<StockReceiptListDtoResponse>> GetStockReceiptsAsync(
        PaginationRequest paginationRequest,
        int? variantId,
        int? staffId,
        DateTime? fromDate,
        DateTime? toDate,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(paginationRequest);

        var repository = _unitOfWork.Repository<RepositoryLayer.Entities.StockReceipt>();

        var pagedReceipts = await repository.GetPagedAsync(
            paginationRequest: paginationRequest,
            filter: receipt =>
                (!variantId.HasValue || receipt.VariantId == variantId.Value) &&
                (!staffId.HasValue || receipt.StaffId == staffId.Value) &&
                (!fromDate.HasValue || receipt.ReceivedDate >= fromDate.Value) &&
                (!toDate.HasValue || receipt.ReceivedDate <= toDate.Value),
            orderBy: query => query.OrderByDescending(receipt => receipt.ReceivedDate),
            includeProperties: "Staff",
            tracked: false,
            cancellationToken: cancellationToken);

        var items = pagedReceipts.Items
            .Select(MapToListDto)
            .ToList();

        return PagedResult<StockReceiptListDtoResponse>.Create(
            items,
            pagedReceipts.Page,
            pagedReceipts.PageSize,
            pagedReceipts.TotalItems);
    }

    public async Task<StockReceiptDtoResponse?> GetStockReceiptByIdAsync(int receiptId, CancellationToken cancellationToken = default)
    {
        var repository = _unitOfWork.Repository<RepositoryLayer.Entities.StockReceipt>();
        var stockReceipt = await repository.GetFirstOrDefaultAsync(
            receipt => receipt.ReceiptId == receiptId,
            includeProperties: "Staff",
            tracked: false);

        return stockReceipt is null ? null : MapToDto(stockReceipt);
    }

    private async Task ReconcilePreOrderAvailabilityAfterStockReceiptAsync(int variantId, CancellationToken cancellationToken)
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

    private static StockReceiptDtoResponse MapToDto(RepositoryLayer.Entities.StockReceipt stockReceipt)
    {
        var receivedDateUtc = stockReceipt.ReceivedDate.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(stockReceipt.ReceivedDate, DateTimeKind.Utc)
            : stockReceipt.ReceivedDate.ToUniversalTime();

        return new StockReceiptDtoResponse
        {
            ReceiptId = stockReceipt.ReceiptId,
            VariantId = stockReceipt.VariantId,
            QuantityReceived = stockReceipt.QuantityReceived,
            ReceivedDate = receivedDateUtc,
            Note = stockReceipt.Note,
            RecordedByUserId = stockReceipt.Staff?.UserId ?? stockReceipt.StaffId,
            RecordedByName = stockReceipt.Staff?.FullName,
            RecordedByRole = stockReceipt.Staff?.Role.ToString()
        };
    }

    private static StockReceiptListDtoResponse MapToListDto(RepositoryLayer.Entities.StockReceipt stockReceipt)
    {
        var receivedDateUtc = stockReceipt.ReceivedDate.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(stockReceipt.ReceivedDate, DateTimeKind.Utc)
            : stockReceipt.ReceivedDate.ToUniversalTime();

        return new StockReceiptListDtoResponse
        {
            ReceiptId = stockReceipt.ReceiptId,
            VariantId = stockReceipt.VariantId,
            QuantityReceived = stockReceipt.QuantityReceived,
            ReceivedDate = receivedDateUtc,
            RecordedByUserId = stockReceipt.Staff?.UserId ?? stockReceipt.StaffId,
            RecordedByName = stockReceipt.Staff?.FullName,
            RecordedByRole = stockReceipt.Staff?.Role.ToString()
        };
    }
}
