namespace ServiceLayer.Contracts.Inventory;

public interface IPreOrderAvailabilityReconciliationService
{
    Task ReconcileAfterStockIncreaseAsync(
        int variantId,
        CancellationToken cancellationToken = default);
}
