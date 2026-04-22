using Microsoft.Extensions.Logging;
using RepositoryLayer.Entities;
using RepositoryLayer.Enums;
using RepositoryLayer.Interfaces;
using ServiceLayer.Contracts.Email;
using ServiceLayer.Contracts.Notifications;

namespace ServiceLayer.Services.Notifications;

public class PreOrderBackInStockNotificationService(
    IUnitOfWork unitOfWork,
    IEmailService emailService,
    ILogger<PreOrderBackInStockNotificationService> logger) : IPreOrderBackInStockNotificationService
{
    private const string BackInStockSubject = "product is back in stock";
    private const string BackInStockBody = "Your preorder item is now back in stock and your order will be processed soon.";

    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IEmailService _emailService = emailService;
    private readonly ILogger<PreOrderBackInStockNotificationService> _logger = logger;

    public async Task HandleStockChangeAsync(
        int variantId,
        int previousQuantity,
        int currentQuantity,
        string source,
        CancellationToken cancellationToken = default)
    {
        if (previousQuantity > 0 || currentQuantity <= 0)
        {
            _logger.LogInformation(
                "Skip notify preorder back-in-stock. Source: {Source}, VariantId: {VariantId}, PreviousQuantity: {PreviousQuantity}, CurrentQuantity: {CurrentQuantity}",
                source,
                variantId,
                previousQuantity,
                currentQuantity);
            return;
        }

        _logger.LogInformation(
            "Trigger notify preorder back-in-stock. Source: {Source}, VariantId: {VariantId}, PreviousQuantity: {PreviousQuantity}, CurrentQuantity: {CurrentQuantity}",
            source,
            variantId,
            previousQuantity,
            currentQuantity);

        try
        {
            var orderRepository = _unitOfWork.Repository<Order>();
            var preorderOrders = await orderRepository.FindAsync(
                filter: order =>
                    order.OrderType == OrderType.PreOrder &&
                    order.OrderStatus == OrderStatus.AwaitingStock &&
                    order.OrderItems.Any(item => item.VariantId == variantId),
                includeProperties: "User",
                tracked: false);

            var recipientEmails = preorderOrders
                .Select(order => order.User.Email?.Trim())
                .Where(email => !string.IsNullOrWhiteSpace(email))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (recipientEmails.Count == 0)
            {
                _logger.LogInformation(
                    "No recipients for preorder back-in-stock notification. Source: {Source}, VariantId: {VariantId}",
                    source,
                    variantId);
                return;
            }

            foreach (var recipientEmail in recipientEmails)
            {
                try
                {
                    await _emailService.SendEmailAsync(
                        recipientEmail!,
                        BackInStockSubject,
                        BackInStockBody,
                        cancellationToken);

                    _logger.LogInformation(
                        "Send success preorder back-in-stock notification. Source: {Source}, VariantId: {VariantId}, RecipientEmail: {RecipientEmail}",
                        source,
                        variantId,
                        recipientEmail);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Send fail preorder back-in-stock notification. Source: {Source}, VariantId: {VariantId}, RecipientEmail: {RecipientEmail}",
                        source,
                        variantId,
                        recipientEmail);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Send fail preorder back-in-stock notification while collecting recipients. Source: {Source}, VariantId: {VariantId}",
                source,
                variantId);
        }
    }
}
