namespace ServiceLayer.DTOs.Payment.Request;

public class MomoCreateGatewayRequestDto
{
    public int PaymentId { get; set; }

    public int OrderId { get; set; }

    public string OrderReference { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public string? OrderInfo { get; set; }
}
