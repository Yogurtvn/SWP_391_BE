using RepositoryLayer.Entities;
using RepositoryLayer.Enums;
using RepositoryLayer.Interfaces;

namespace ServiceLayer.Utilities;

internal static class OrderWorkflowMutations
{
    internal readonly record struct InventoryQuantityTransition(int VariantId, int PreviousQuantity, int CurrentQuantity);
    internal readonly record struct PreOrderProcessingTransitionResult(bool Succeeded, string FailureReason)
    {
        public static readonly PreOrderProcessingTransitionResult Success = new(true, string.Empty);

        public static PreOrderProcessingTransitionResult Failed(string failureReason) => new(false, failureReason);
    }

    public static async Task<IReadOnlyCollection<InventoryQuantityTransition>> CancelOrderAsync(
        IUnitOfWork unitOfWork,
        Order order,
        int? updatedByUserId,
        string note,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var inventoryTransitions = new Dictionary<int, InventoryQuantityTransition>();

        if (ShouldRestoreInventoryOnCancel(order))
        {
            foreach (var orderItem in order.OrderItems)
            {
                var inventory = orderItem.Variant.Inventory;

                if (inventory is null)
                {
                    inventory = new Inventory
                    {
                        VariantId = orderItem.VariantId,
                        Quantity = 0,
                        IsPreOrderAllowed = false
                    };

                    await unitOfWork.Repository<Inventory>().AddAsync(inventory);
                    orderItem.Variant.Inventory = inventory;
                }

                var previousQuantity = inventory.Quantity;
                inventory.Quantity += orderItem.Quantity;
                var currentQuantity = inventory.Quantity;

                if (inventoryTransitions.TryGetValue(orderItem.VariantId, out var transition))
                {
                    inventoryTransitions[orderItem.VariantId] = transition with { CurrentQuantity = currentQuantity };
                }
                else
                {
                    inventoryTransitions[orderItem.VariantId] = new InventoryQuantityTransition(
                        orderItem.VariantId,
                        previousQuantity,
                        currentQuantity);
                }
            }
        }

        order.OrderStatus = OrderStatus.Cancelled;
        order.ShippingStatus = null;
        order.UpdatedAt = now;

        if (updatedByUserId.HasValue)
        {
            order.StaffId = updatedByUserId.Value;
        }

        order.OrderStatusHistories.Add(new OrderStatusHistory
        {
            OrderStatus = OrderStatus.Cancelled,
            UpdatedByUserId = updatedByUserId,
            Note = note,
            UpdatedAt = now
        });

        foreach (var payment in order.Payments.Where(payment =>
                     payment.PaymentStatus != PaymentStatus.Completed
                     && payment.PaymentStatus != PaymentStatus.Failed))
        {
            payment.PaymentStatus = PaymentStatus.Failed;
            payment.PaidAt = null;
            payment.PaymentHistories.Add(new PaymentHistory
            {
                PaymentStatus = PaymentStatus.Failed,
                Notes = "Payment closed because the order was cancelled.",
                CreatedAt = now
            });
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return inventoryTransitions.Values.ToArray();
    }

    public static async Task<PreOrderProcessingTransitionResult> MovePreOrderAwaitingStockToProcessingInternalAsync(
        IUnitOfWork unitOfWork,
        Order order,
        OrderStatusTransitionContext transitionContext,
        string note,
        CancellationToken cancellationToken)
    {
        if (transitionContext is not (OrderStatusTransitionContext.StockReceiptWorkflow or OrderStatusTransitionContext.VariantUpdateWorkflow))
        {
            return PreOrderProcessingTransitionResult.Failed("Transition context is not supported.");
        }

        if (!OrderWorkflowPolicies.CanTransitionOrderStatus(
                order.OrderType,
                order.OrderStatus,
                OrderStatus.Processing,
                transitionContext))
        {
            return PreOrderProcessingTransitionResult.Failed("Transition policy rejected this request.");
        }

        var requiredQuantities = OrderWorkflowPolicies.GetRequiredVariantQuantities(order);

        if (requiredQuantities.Count == 0)
        {
            return PreOrderProcessingTransitionResult.Failed("Order has no items to reserve inventory.");
        }

        foreach (var requirement in requiredQuantities)
        {
            if (requirement.Value <= 0)
            {
                return PreOrderProcessingTransitionResult.Failed(
                    $"Order has invalid required quantity for variant {requirement.Key}.");
            }

            var reserved = await unitOfWork.TryDeductInventoryAsync(
                requirement.Key,
                requirement.Value,
                cancellationToken);

            if (!reserved)
            {
                return PreOrderProcessingTransitionResult.Failed(
                    $"Inventory deduction failed for variant {requirement.Key}.");
            }
        }

        var now = DateTime.UtcNow;
        order.OrderStatus = OrderStatus.Processing;
        order.UpdatedAt = now;
        order.OrderStatusHistories.Add(new OrderStatusHistory
        {
            OrderStatus = OrderStatus.Processing,
            UpdatedByUserId = null,
            Note = note,
            UpdatedAt = now
        });

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return PreOrderProcessingTransitionResult.Success;
    }

    private static bool ShouldRestoreInventoryOnCancel(Order order)
    {
        return order.OrderType != OrderType.PreOrder;
    }
}
