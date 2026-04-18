namespace ServiceLayer.DTOs.Orders;

public class CheckoutOrderResponse
{
    public int OrderId { get; set; }

    public decimal TotalAmount { get; set; }

    public string OrderStatus { get; set; } = string.Empty;

    public CheckoutPaymentResponse Payment { get; set; } = new();
}

public class BuyNowOrderResponse
{
    public int OrderId { get; set; }

    public string OrderType { get; set; } = string.Empty;

    public string OrderStatus { get; set; } = string.Empty;

    public CheckoutPaymentResponse Payment { get; set; } = new();
}

public class CheckoutPaymentResponse
{
    public int PaymentId { get; set; }

    public string PaymentStatus { get; set; } = string.Empty;

    public string? PayUrl { get; set; }

    public string? Deeplink { get; set; }

    public string? QrCodeUrl { get; set; }
}
