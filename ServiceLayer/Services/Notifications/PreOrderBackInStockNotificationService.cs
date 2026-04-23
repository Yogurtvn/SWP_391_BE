using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RepositoryLayer.Entities;
using RepositoryLayer.Enums;
using RepositoryLayer.Interfaces;
using ServiceLayer.Configuration;
using ServiceLayer.Contracts.Email;
using ServiceLayer.Contracts.Notifications;

namespace ServiceLayer.Services.Notifications;

public class PreOrderBackInStockNotificationService(
    IUnitOfWork unitOfWork,
    IEmailService emailService,
    IOptions<EmailSettings> emailOptions,
    ILogger<PreOrderBackInStockNotificationService> logger) : IPreOrderBackInStockNotificationService
{
    private const string BackInStockSubject = "[E-World] S\u1EA3n ph\u1EA9m b\u1EA1n \u0111\u1EB7t tr\u01B0\u1EDBc \u0111\u00E3 v\u1EC1 h\u00E0ng";

    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IEmailService _emailService = emailService;
    private readonly EmailSettings _emailSettings = emailOptions.Value;
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
                includeProperties: "User,OrderItems.Variant.Product,OrderItems.Variant.Inventory",
                tracked: false);

            var recipients = preorderOrders
                .Select(order => MapRecipient(order, variantId))
                .Where(recipient => recipient is not null)
                .Cast<BackInStockRecipient>()
                .GroupBy(recipient => recipient.Email, StringComparer.OrdinalIgnoreCase)
                .Select(group => group
                    .OrderByDescending(recipient => recipient.OrderId)
                    .First())
                .ToList();

            if (recipients.Count == 0)
            {
                _logger.LogInformation(
                    "No recipients for preorder back-in-stock notification. Source: {Source}, VariantId: {VariantId}",
                    source,
                    variantId);
                return;
            }

            var updatedAt = DateTime.Now;

            foreach (var recipient in recipients)
            {
                try
                {
                    var body = PreOrderBackInStockEmailTemplateBuilder.Build(
                        new PreOrderBackInStockEmailTemplateData
                        {
                            CustomerName = recipient.CustomerName,
                            OrderId = recipient.OrderId,
                            ProductName = recipient.ProductName,
                            Sku = recipient.Sku,
                            VariantInfo = recipient.VariantInfo,
                            ExpectedRestockDate = recipient.ExpectedRestockDate,
                            UpdatedAt = updatedAt,
                            OrderTrackingUrl = _emailSettings.OrderTrackingUrl
                        });

                    await _emailService.SendEmailAsync(
                        recipient.Email,
                        BackInStockSubject,
                        body,
                        cancellationToken,
                        isBodyHtml: true);

                    _logger.LogInformation(
                        "Send success preorder back-in-stock notification. Source: {Source}, VariantId: {VariantId}, RecipientEmail: {RecipientEmail}",
                        source,
                        variantId,
                        recipient.Email);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Send fail preorder back-in-stock notification. Source: {Source}, VariantId: {VariantId}, RecipientEmail: {RecipientEmail}",
                        source,
                        variantId,
                        recipient.Email);
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

    private static BackInStockRecipient? MapRecipient(Order order, int variantId)
    {
        var email = order.User.Email?.Trim();
        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        var orderItem = order.OrderItems.FirstOrDefault(item => item.VariantId == variantId);
        var variant = orderItem?.Variant;

        return new BackInStockRecipient
        {
            Email = email,
            CustomerName = Normalize(order.User.FullName),
            OrderId = order.OrderId,
            ProductName = Normalize(variant?.Product.ProductName),
            Sku = Normalize(variant?.Sku),
            VariantInfo = BuildVariantInfo(variant),
            ExpectedRestockDate = variant?.Inventory?.ExpectedRestockDate
        };
    }

    private static string? BuildVariantInfo(ProductVariant? variant)
    {
        if (variant is null)
        {
            return null;
        }

        var variantPieces = new List<string>();

        if (!string.IsNullOrWhiteSpace(variant.Color))
        {
            variantPieces.Add($"M\u00E0u: {variant.Color.Trim()}");
        }

        if (!string.IsNullOrWhiteSpace(variant.Size))
        {
            variantPieces.Add($"Size: {variant.Size.Trim()}");
        }

        if (!string.IsNullOrWhiteSpace(variant.FrameType))
        {
            variantPieces.Add($"D\u00F2ng g\u1ECDng: {variant.FrameType.Trim()}");
        }

        return variantPieces.Count == 0 ? null : string.Join(" | ", variantPieces);
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private sealed class BackInStockRecipient
    {
        public required string Email { get; init; }

        public string? CustomerName { get; init; }

        public int OrderId { get; init; }

        public string? ProductName { get; init; }

        public string? Sku { get; init; }

        public string? VariantInfo { get; init; }

        public DateTime? ExpectedRestockDate { get; init; }
    }
}
