namespace ServiceLayer.DTOs.Orders;

public class CancelOrderResult
{
    public bool Succeeded { get; set; }

    public string? ErrorCode { get; set; }

    public string Message { get; set; } = string.Empty;

    public OrderDetailResponse? Order { get; set; }
}
