namespace ServiceLayer.DTOs.Orders;

public class OrderSummaryResponse
{
    public int OrderId { get; set; }

    public string OrderType { get; set; } = string.Empty;

    public string OrderStatus { get; set; } = string.Empty;

    public string? ShippingStatus { get; set; }

    public decimal TotalAmount { get; set; }

    public int ItemCount { get; set; }

    public string ReceiverName { get; set; } = string.Empty;

    public string? PaymentMethod { get; set; }

    public string? PaymentStatus { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
