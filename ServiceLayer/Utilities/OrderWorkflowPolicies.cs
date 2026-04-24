using RepositoryLayer.Entities;
using RepositoryLayer.Enums;

namespace ServiceLayer.Utilities;

internal enum OrderStatusTransitionContext : byte
{
    Default = 0,
    StaffPatch = 1,
    StockReceiptWorkflow = 2,
    VariantUpdateWorkflow = 3
}

internal static class OrderWorkflowPolicies
{
    private static readonly Dictionary<OrderStatus, HashSet<OrderStatus>> ReadyOrderTransitions = new()
    {
        [OrderStatus.Pending] = [OrderStatus.Processing, OrderStatus.Cancelled],
        [OrderStatus.Processing] = [OrderStatus.Shipped, OrderStatus.Cancelled],
        [OrderStatus.Shipped] = [OrderStatus.Completed, OrderStatus.Cancelled],
        [OrderStatus.Completed] = [],
        [OrderStatus.Cancelled] = []
    };

    private static readonly Dictionary<OrderStatus, HashSet<OrderStatus>> PreOrderTransitions = new()
    {
        [OrderStatus.Pending] = [OrderStatus.AwaitingStock, OrderStatus.Cancelled],
        [OrderStatus.AwaitingStock] = [OrderStatus.Processing, OrderStatus.Cancelled],
        [OrderStatus.Processing] = [OrderStatus.Shipped, OrderStatus.Cancelled],
        [OrderStatus.Shipped] = [OrderStatus.Completed],
        [OrderStatus.Completed] = [],
        [OrderStatus.Cancelled] = []
    };

    private static readonly Dictionary<OrderStatus, HashSet<OrderStatus>> PrescriptionOrderTransitions = new()
    {
        [OrderStatus.Pending] = [OrderStatus.Processing, OrderStatus.Cancelled],
        [OrderStatus.Processing] = [OrderStatus.Shipped, OrderStatus.Cancelled],
        [OrderStatus.Shipped] = [OrderStatus.Completed],
        [OrderStatus.Completed] = [],
        [OrderStatus.Cancelled] = []
    };

    private static readonly Dictionary<PrescriptionStatus, HashSet<PrescriptionStatus>> PrescriptionStatusTransitions = new()
    {
        [PrescriptionStatus.Submitted] = [PrescriptionStatus.Reviewing, PrescriptionStatus.Approved, PrescriptionStatus.Rejected],
        [PrescriptionStatus.Reviewing] = [PrescriptionStatus.Approved, PrescriptionStatus.Rejected],
        [PrescriptionStatus.Approved] = [],
        [PrescriptionStatus.Rejected] = []
    };

    private static readonly Dictionary<OrderStatus, HashSet<ShippingStatus>> ShippingStatusByOrderStatus = new()
    {
        [OrderStatus.Processing] =
        [
            ShippingStatus.Pending,
            ShippingStatus.Picking,
            ShippingStatus.Delivering,
            ShippingStatus.Delivered,
            ShippingStatus.Failed
        ],
        [OrderStatus.Shipped] =
        [
            ShippingStatus.Picking,
            ShippingStatus.Delivering,
            ShippingStatus.Delivered,
            ShippingStatus.Failed
        ]
    };

    public static IReadOnlyDictionary<OrderStatus, HashSet<OrderStatus>> GetOrderTransitions(OrderType orderType)
    {
        return orderType switch
        {
            OrderType.Ready => ReadyOrderTransitions,
            OrderType.PreOrder => PreOrderTransitions,
            OrderType.Prescription => PrescriptionOrderTransitions,
            _ => throw new ArgumentOutOfRangeException(nameof(orderType), orderType, "Unsupported order type")
        };
    }

    public static IReadOnlyDictionary<OrderStatus, HashSet<OrderStatus>> GetReadyOrderTransitions() => ReadyOrderTransitions;

    public static IReadOnlyDictionary<OrderStatus, HashSet<OrderStatus>> GetPreOrderTransitions() => PreOrderTransitions;

    public static IReadOnlyDictionary<OrderStatus, HashSet<OrderStatus>> GetPrescriptionOrderTransitions() => PrescriptionOrderTransitions;

    public static IReadOnlyDictionary<PrescriptionStatus, HashSet<PrescriptionStatus>> GetPrescriptionStatusTransitions() => PrescriptionStatusTransitions;

    public static bool CanTransitionOrderStatus(
        OrderType orderType,
        OrderStatus currentStatus,
        OrderStatus nextStatus,
        OrderStatusTransitionContext context = OrderStatusTransitionContext.Default)
    {
        if (currentStatus == nextStatus)
        {
            return false;
        }

        if (orderType == OrderType.PreOrder
            && currentStatus == OrderStatus.AwaitingStock
            && nextStatus == OrderStatus.Processing)
        {
            return context is OrderStatusTransitionContext.StockReceiptWorkflow or OrderStatusTransitionContext.VariantUpdateWorkflow;
        }

        return GetOrderTransitions(orderType).TryGetValue(currentStatus, out var allowedStatuses)
               && allowedStatuses.Contains(nextStatus);
    }

    public static bool IsPreOrderStockRestorationTransition(OrderType orderType, OrderStatus currentStatus, OrderStatus nextStatus)
    {
        return orderType == OrderType.PreOrder
               && currentStatus == OrderStatus.AwaitingStock
               && nextStatus == OrderStatus.Processing;
    }

    public static bool CanTransitionPrescriptionStatus(PrescriptionStatus currentStatus, PrescriptionStatus nextStatus)
    {
        if (currentStatus == nextStatus)
        {
            return false;
        }

        return PrescriptionStatusTransitions.TryGetValue(currentStatus, out var allowedStatuses)
               && allowedStatuses.Contains(nextStatus);
    }

    public static bool CanUpdateShippingStatus(OrderStatus orderStatus, ShippingStatus shippingStatus)
    {
        return ShippingStatusByOrderStatus.TryGetValue(orderStatus, out var allowedStatuses)
               && allowedStatuses.Contains(shippingStatus);
    }

    public static bool IsOnlinePaymentMethod(PaymentMethod paymentMethod)
    {
        return paymentMethod != PaymentMethod.COD;
    }

    public static bool HasCompletedOnlinePayment(IEnumerable<Payment> payments)
    {
        return payments.Any(payment =>
            IsOnlinePaymentMethod(payment.PaymentMethod)
            && payment.PaymentStatus == PaymentStatus.Completed);
    }

    public static bool HasOnlinePayment(IEnumerable<Payment> payments)
    {
        return payments.Any(payment => IsOnlinePaymentMethod(payment.PaymentMethod));
    }

    public static bool CanCancelByPaymentRule(IEnumerable<Payment> payments)
    {
        var paymentList = payments.ToList();

        if (paymentList.Count == 0)
        {
            return true;
        }

        if (HasCompletedOnlinePayment(paymentList))
        {
            return false;
        }

        var hasNotCompletedPayment = paymentList.Any(payment => payment.PaymentStatus != PaymentStatus.Completed);
        var hasCodPending = paymentList.Any(payment =>
            payment.PaymentMethod == PaymentMethod.COD
            && payment.PaymentStatus == PaymentStatus.Pending);

        return hasNotCompletedPayment || hasCodPending;
    }

    public static bool CanMovePreOrderToAwaitingStock(IEnumerable<Payment> payments)
    {
        var paymentList = payments.ToList();

        if (!HasOnlinePayment(paymentList))
        {
            return true;
        }

        return HasCompletedOnlinePayment(paymentList);
    }

    public static bool CanCustomerCancelByOrderStatus(Order order)
    {
        return order.OrderType switch
        {
            OrderType.Ready => order.OrderStatus == OrderStatus.Pending,
            OrderType.PreOrder => order.OrderStatus is OrderStatus.Pending or OrderStatus.AwaitingStock,
            OrderType.Prescription => order.OrderStatus == OrderStatus.Pending,
            _ => false
        };
    }

    public static bool IsPrescriptionCustomerCancellationWindowOpen(Order order)
    {
        if (order.OrderType != OrderType.Prescription || order.OrderStatus != OrderStatus.Pending)
        {
            return false;
        }

        var prescriptions = order.OrderItems
            .Where(orderItem => orderItem.Prescription is not null)
            .Select(orderItem => orderItem.Prescription!)
            .ToList();

        return prescriptions.Count > 0
               && prescriptions.All(prescription => prescription.PrescriptionStatus == PrescriptionStatus.Submitted);
    }

    public static bool AreAllPrescriptionItemsApproved(Order order)
    {
        if (order.OrderType != OrderType.Prescription)
        {
            return true;
        }

        var prescriptions = order.OrderItems
            .Where(orderItem => orderItem.Prescription is not null)
            .Select(orderItem => orderItem.Prescription!)
            .ToList();

        return prescriptions.Count > 0
               && prescriptions.All(prescription => prescription.PrescriptionStatus == PrescriptionStatus.Approved);
    }

    public static IReadOnlyDictionary<int, int> GetRequiredVariantQuantities(Order order)
    {
        return order.OrderItems
            .GroupBy(orderItem => orderItem.VariantId)
            .ToDictionary(group => group.Key, group => group.Sum(item => item.Quantity));
    }

    public static bool HasSufficientInventoryForPreOrder(Order order)
    {
        if (order.OrderType != OrderType.PreOrder)
        {
            return true;
        }

        var requiredQuantities = GetRequiredVariantQuantities(order);

        foreach (var requirement in requiredQuantities)
        {
            var inventoryQuantity = order.OrderItems
                .Where(orderItem => orderItem.VariantId == requirement.Key)
                .Select(orderItem => orderItem.Variant.Inventory?.Quantity ?? 0)
                .DefaultIfEmpty(0)
                .First();

            if (inventoryQuantity < requirement.Value)
            {
                return false;
            }
        }

        return true;
    }
}
