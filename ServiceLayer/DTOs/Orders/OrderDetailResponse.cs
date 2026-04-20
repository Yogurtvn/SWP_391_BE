namespace ServiceLayer.DTOs.Orders;

public class OrderDetailResponse
{
    public int OrderId { get; set; }

    public int UserId { get; set; }

    public string OrderType { get; set; } = string.Empty;

    public string OrderStatus { get; set; } = string.Empty;

    public decimal TotalAmount { get; set; }

    public string ReceiverName { get; set; } = string.Empty;

    public string ReceiverPhone { get; set; } = string.Empty;

    public string ShippingAddress { get; set; } = string.Empty;

    public string? ShippingCode { get; set; }

    public string? ShippingStatus { get; set; }

    public DateTime? ExpectedDeliveryDate { get; set; }

    public int? StaffId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public List<OrderItemResponse> Items { get; set; } = [];

    public OrderPaymentResponse? Payment { get; set; }

    public List<OrderStatusHistoryResponse> StatusHistory { get; set; } = [];
}

public class OrderItemResponse
{
    public int OrderItemId { get; set; }

    public int VariantId { get; set; }

    public int ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public string Sku { get; set; } = string.Empty;

    public string? VariantColor { get; set; }

    public string? SelectedColor { get; set; }

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal OriginalUnitPrice { get; set; }

    public decimal DiscountPercent { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal FinalUnitPrice { get; set; }

    public string? PromotionNameSnapshot { get; set; }

    public decimal LineTotal { get; set; }
}

public class OrderPaymentResponse
{
    public int PaymentId { get; set; }

    public decimal Amount { get; set; }

    public string PaymentMethod { get; set; } = string.Empty;

    public string PaymentStatus { get; set; } = string.Empty;

    public DateTime? PaidAt { get; set; }

    public List<OrderPaymentHistoryResponse> Histories { get; set; } = [];
}

public class OrderPaymentHistoryResponse
{
    public int PaymentHistoryId { get; set; }

    public string PaymentStatus { get; set; } = string.Empty;

    public string? TransactionCode { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }
}

public class OrderStatusHistoryResponse
{
    public int HistoryId { get; set; }

    public string OrderStatus { get; set; } = string.Empty;

    public int? UpdatedByUserId { get; set; }

    public string? UpdatedByName { get; set; }

    public string? Note { get; set; }

    public DateTime UpdatedAt { get; set; }
}
