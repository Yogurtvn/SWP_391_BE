using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RepositoryLayer.Entities;
using RepositoryLayer.Enums;
using RepositoryLayer.Interfaces;
using ServiceLayer.Configuration;
using ServiceLayer.Contracts.Email;
using ServiceLayer.Contracts.Notifications;
using ServiceLayer.Utilities;

namespace ServiceLayer.Services.Notifications;

public class PreOrderBackInStockNotificationService(
    IUnitOfWork unitOfWork,
    IEmailService emailService,
    IOptions<EmailSettings> emailOptions,
    ILogger<PreOrderBackInStockNotificationService> logger) : IPreOrderBackInStockNotificationService
{
    private const string BackInStockSubject = "[E-World] S\u1EA3n ph\u1EA9m b\u1EA1n \u0111\u1EB7t tr\u01B0\u1EDBc \u0111\u00E3 v\u1EC1 h\u00E0ng";
    private const string StockReceiptSource = "stock-receipt:create";
    private const string VariantUpdateSource = "variant:update";

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
        var transitionContext = ResolveTransitionContext(source);
        var isStockRestorationWorkflow = transitionContext is not OrderStatusTransitionContext.Default;

        var shouldHandle =
            isStockRestorationWorkflow
                // Stock restoration workflows care about any increase (not only crossing 0).
                ? currentQuantity > previousQuantity
                // Generic flows notify only when stock comes back from zero/out-of-stock.
                : previousQuantity <= 0 && currentQuantity > 0;

        if (!shouldHandle)
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
            // Only pre-orders still waiting for stock are eligible for this notification workflow.
            var preorderOrders = await orderRepository.FindAsync(
                filter: order =>
                    order.OrderType == OrderType.PreOrder &&
                    order.OrderStatus == OrderStatus.AwaitingStock &&
                    order.OrderItems.Any(item => item.VariantId == variantId),
                includeProperties: "User,OrderItems.Variant.Product,OrderItems.Variant.Inventory,Payments,OrderStatusHistories",
                tracked: true);

            var orderRecipientPairs = preorderOrders
                .Select(order => new
                {
                    Order = order,
                    Recipient = MapRecipient(order, variantId)
                })
                .Where(item => item.Recipient is not null)
                .Select(item => new
                {
                    item.Order,
                    Recipient = item.Recipient!
                })
                .OrderBy(item => item.Order.OrderId)
                .ToList();

            if (orderRecipientPairs.Count == 0)
            {
                _logger.LogInformation(
                    "No recipients for preorder back-in-stock notification. Source: {Source}, VariantId: {VariantId}",
                    source,
                    variantId);
                return;
            }

            var updatedAt = DateTime.Now;

            foreach (var item in orderRecipientPairs)
            {
                try
                {
                    if (isStockRestorationWorkflow)
                    {
                        // Important: for auto workflows, re-check full business eligibility before notifying and mutating.
                        var eligibility = await EvaluateAutoTransitionEligibilityAsync(
                            item.Order,
                            transitionContext,
                            source);

                        if (!eligibility.CanTransition)
                        {
                            _logger.LogInformation(
                                "Skip preorder stock-restoration workflow. Source: {Source}, OrderId: {OrderId}, Reason: {Reason}",
                                source,
                                item.Order.OrderId,
                                eligibility.Reason);
                            continue;
                        }
                    }

                    var body = PreOrderBackInStockEmailTemplateBuilder.Build(
                        new PreOrderBackInStockEmailTemplateData
                        {
                            CustomerName = item.Recipient.CustomerName,
                            OrderId = item.Recipient.OrderId,
                            ProductName = item.Recipient.ProductName,
                            Sku = item.Recipient.Sku,
                            VariantInfo = item.Recipient.VariantInfo,
                            ExpectedRestockDate = item.Recipient.ExpectedRestockDate,
                            UpdatedAt = updatedAt,
                            OrderTrackingUrl = _emailSettings.OrderTrackingUrl
                        });

                    await _emailService.SendEmailAsync(
                        item.Recipient.Email,
                        BackInStockSubject,
                        body,
                        cancellationToken,
                        isBodyHtml: true);

                    _logger.LogInformation(
                        "Send success preorder back-in-stock notification. Source: {Source}, VariantId: {VariantId}, RecipientEmail: {RecipientEmail}",
                        source,
                        variantId,
                        item.Recipient.Email);

                    if (isStockRestorationWorkflow)
                    {
                        // Order flow: after successful notification, workflow may auto move AwaitingStock -> Processing.
                        await TryAutoMoveAwaitingStockOrderToProcessingAsync(
                            item.Order,
                            source,
                            transitionContext,
                            cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Send fail preorder back-in-stock notification. Source: {Source}, VariantId: {VariantId}, RecipientEmail: {RecipientEmail}",
                        source,
                        variantId,
                        item.Recipient.Email);
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

    private async Task TryAutoMoveAwaitingStockOrderToProcessingAsync(
        Order order,
        string source,
        OrderStatusTransitionContext transitionContext,
        CancellationToken cancellationToken)
    {
        var eligibility = await EvaluateAutoTransitionEligibilityAsync(
            order,
            transitionContext,
            source);

        if (!eligibility.CanTransition)
        {
            _logger.LogInformation(
                "Skip preorder auto transition to processing. Source: {Source}, OrderId: {OrderId}, Reason: {Reason}",
                source,
                order.OrderId,
                eligibility.Reason);
            return;
        }

        try
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            var note = transitionContext == OrderStatusTransitionContext.StockReceiptWorkflow
                ? "Order moved to processing automatically after stock receipt and back-in-stock email."
                : "Order moved to processing automatically after variant stock restoration and back-in-stock email.";

            var transitionResult = await OrderWorkflowMutations.MovePreOrderAwaitingStockToProcessingInternalAsync(
                _unitOfWork,
                order,
                transitionContext,
                note,
                cancellationToken);

            if (!transitionResult.Succeeded)
            {
                // Why: keep transition atomic; no partial inventory deductions are committed when mutation fails.
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                _logger.LogWarning(
                    "Preorder auto transition blocked. Source: {Source}, OrderId: {OrderId}, Reason: {Reason}",
                    source,
                    order.OrderId,
                    transitionResult.FailureReason);
                return;
            }

            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation(
                "Preorder auto transitioned to processing. Source: {Source}, OrderId: {OrderId}",
                source,
                order.OrderId);
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(
                ex,
                "Fail preorder auto transition to processing. Source: {Source}, OrderId: {OrderId}",
                source,
                order.OrderId);
        }
    }

    private async Task<(bool CanTransition, string Reason)> EvaluateAutoTransitionEligibilityAsync(
        Order order,
        OrderStatusTransitionContext transitionContext,
        string source)
    {
        // Policy guard: source context must be authorized for AwaitingStock -> Processing transition.
        if (!OrderWorkflowPolicies.CanTransitionOrderStatus(
                order.OrderType,
                order.OrderStatus,
                OrderStatus.Processing,
                transitionContext))
        {
            return (false, "transition policy rejected this source");
        }

        if (!OrderWorkflowPolicies.CanMovePreOrderToAwaitingStock(order.Payments))
        {
            // Payment guard: pre-order payment checkpoint must already be satisfied.
            return (false, "payment is not ready");
        }

        if (!OrderWorkflowPolicies.HasSufficientInventoryForPreOrder(order))
        {
            // Soft guard: skip costly mutation when current in-memory inventory is obviously insufficient.
            return (false, "inventory pre-check is insufficient");
        }

        if (!await HasValidStockReceiptForOrderAsync(order))
        {
            // Business guard: require stock receipt evidence to explain why workflow is allowed to proceed.
            return (false, "valid stock receipt is missing");
        }

        if (transitionContext == OrderStatusTransitionContext.VariantUpdateWorkflow
            && !string.Equals(source, VariantUpdateSource, StringComparison.OrdinalIgnoreCase))
        {
            return (false, "source is not variant update API workflow");
        }

        return (true, "eligible");
    }

    private static OrderStatusTransitionContext ResolveTransitionContext(string source)
    {
        if (string.Equals(source, StockReceiptSource, StringComparison.OrdinalIgnoreCase))
        {
            return OrderStatusTransitionContext.StockReceiptWorkflow;
        }

        if (string.Equals(source, VariantUpdateSource, StringComparison.OrdinalIgnoreCase))
        {
            return OrderStatusTransitionContext.VariantUpdateWorkflow;
        }

        return OrderStatusTransitionContext.Default;
    }

    private async Task<bool> HasValidStockReceiptForOrderAsync(Order order)
    {
        var stockReceiptRepository = _unitOfWork.Repository<StockReceipt>();
        var requiredVariantQuantities = OrderWorkflowPolicies.GetRequiredVariantQuantities(order);
        if (requiredVariantQuantities.Count == 0)
        {
            return false;
        }

        var variantIds = requiredVariantQuantities.Keys.ToArray();
        // Demo note: only receipts after entering AwaitingStock are counted for this restoration workflow.
        var guardStartDate = ResolveStockReceiptGuardStartDate(order);
        var stockReceipts = await stockReceiptRepository.FindAsync(
            filter: receipt =>
                variantIds.Contains(receipt.VariantId)
                && receipt.QuantityReceived > 0
                && receipt.ReceivedDate >= guardStartDate,
            tracked: false);

        var coveredVariantIds = stockReceipts
            .Select(receipt => receipt.VariantId)
            .Distinct()
            .ToHashSet();

        foreach (var variantId in variantIds)
        {
            if (!coveredVariantIds.Contains(variantId))
            {
                return false;
            }
        }

        return true;
    }

    private static DateTime ResolveStockReceiptGuardStartDate(Order order)
    {
        var latestAwaitingStockEntry = order.OrderStatusHistories
            .Where(history => history.OrderStatus == OrderStatus.AwaitingStock)
            .OrderByDescending(history => history.UpdatedAt)
            .FirstOrDefault();

        if (latestAwaitingStockEntry is null)
        {
            return order.CreatedAt;
        }

        // Why: if order re-enters AwaitingStock later, we only trust receipts from the latest waiting period onward.
        return latestAwaitingStockEntry.UpdatedAt > order.CreatedAt
            ? latestAwaitingStockEntry.UpdatedAt
            : order.CreatedAt;
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
