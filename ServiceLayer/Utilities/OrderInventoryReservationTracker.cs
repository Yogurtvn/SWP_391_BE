using RepositoryLayer.Entities;
using RepositoryLayer.Enums;

namespace ServiceLayer.Utilities;

internal static class OrderInventoryReservationTracker
{
    private const string InventoryReservedTrueMarker = "[sys:inventoryReserved=true]";
    private const string InventoryReservedFalseMarker = "[sys:inventoryReserved=false]";

    public static bool ShouldReserveInventoryAtCheckout(OrderType orderType, PaymentMethod paymentMethod)
    {
        if (orderType == OrderType.PreOrder)
        {
            return false;
        }

        // Ready/Prescription paid online reserve stock only after payment is confirmed.
        return paymentMethod == PaymentMethod.COD;
    }

    public static string BuildInitialPaymentNote(bool inventoryReservedAtCheckout)
    {
        return inventoryReservedAtCheckout
            ? $"Payment created. {InventoryReservedTrueMarker}"
            : $"Payment created. {InventoryReservedFalseMarker}";
    }

    public static string BuildInventoryReservedAfterOnlinePaymentNote()
    {
        return $"Inventory reserved after online payment confirmation. {InventoryReservedTrueMarker}";
    }

    public static bool ShouldRestoreInventoryOnCancel(Order order)
    {
        if (order.OrderType == OrderType.PreOrder)
        {
            return false;
        }

        return HasInventoryBeenReserved(order);
    }

    public static bool ShouldAutoCancelOnOnlinePaymentFailure(Order order)
    {
        if (order.OrderType == OrderType.PreOrder)
        {
            return true;
        }

        return HasInventoryBeenReserved(order);
    }

    public static bool HasInventoryBeenReserved(Order order)
    {
        if (order.OrderType == OrderType.PreOrder)
        {
            return false;
        }

        var marker = TryGetLatestInventoryReservationMarker(order);

        if (marker.HasValue)
        {
            return marker.Value;
        }

        // Legacy fallback: historical Ready/Prescription orders reserved stock at checkout.
        return true;
    }

    private static bool? TryGetLatestInventoryReservationMarker(Order order)
    {
        var histories = order.Payments
            .SelectMany(payment => payment.PaymentHistories)
            .OrderByDescending(history => history.CreatedAt)
            .ThenByDescending(history => history.PaymentHistoryId);

        foreach (var history in histories)
        {
            if (TryParseInventoryReservationMarker(history.Notes, out var marker))
            {
                return marker;
            }
        }

        return null;
    }

    private static bool TryParseInventoryReservationMarker(string? note, out bool reserved)
    {
        reserved = false;

        if (string.IsNullOrWhiteSpace(note))
        {
            return false;
        }

        if (note.Contains(InventoryReservedTrueMarker, StringComparison.Ordinal))
        {
            reserved = true;
            return true;
        }

        if (note.Contains(InventoryReservedFalseMarker, StringComparison.Ordinal))
        {
            reserved = false;
            return true;
        }

        return false;
    }
}
