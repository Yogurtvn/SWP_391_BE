namespace ServiceLayer.Contracts.Notifications;

public interface IPreOrderBackInStockNotificationService
{
    Task HandleStockChangeAsync(
        int variantId,
        int previousQuantity,
        int currentQuantity,
        string source,
        CancellationToken cancellationToken = default);
}
