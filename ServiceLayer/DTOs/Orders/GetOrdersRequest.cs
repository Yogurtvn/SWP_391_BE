namespace ServiceLayer.DTOs.Orders;

public class GetOrdersRequest
{
    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 20;

    public string? OrderType { get; set; }

    public string? OrderStatus { get; set; }

    public string? ShippingStatus { get; set; }

    public string? PaymentStatus { get; set; }

    public DateTime? FromDate { get; set; }

    public DateTime? ToDate { get; set; }

    public string? SortBy { get; set; }

    public string? SortOrder { get; set; }
}
