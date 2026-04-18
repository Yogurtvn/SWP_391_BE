namespace ServiceLayer.DTOs.Orders;

public class OrderCancelResponse
{
    public string Message { get; set; } = string.Empty;

    public string OrderStatus { get; set; } = string.Empty;
}

public class OrderStatusUpdatedResponse
{
    public string Message { get; set; } = string.Empty;

    public string OrderStatus { get; set; } = string.Empty;
}

public class ShippingStatusUpdatedResponse
{
    public string Message { get; set; } = string.Empty;

    public string ShippingStatus { get; set; } = string.Empty;
}

public class OrderStatusHistoriesResponse
{
    public List<OrderStatusHistoryListItemResponse> Items { get; set; } = [];
}

public class OrderStatusHistoryListItemResponse
{
    public int HistoryId { get; set; }

    public string OrderStatus { get; set; } = string.Empty;

    public string? Note { get; set; }

    public DateTime UpdatedAt { get; set; }
}
