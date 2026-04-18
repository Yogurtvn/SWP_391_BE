namespace ServiceLayer.DTOs.Payment.Request;

public class PayOsCreateGatewayRequestDto
{
    public int PaymentId { get; set; }

    public int OrderId { get; set; }

    public long OrderCode { get; set; }

    public decimal Amount { get; set; }

    public string Description { get; set; } = string.Empty;
}
