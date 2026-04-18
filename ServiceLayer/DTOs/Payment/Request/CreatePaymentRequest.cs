namespace ServiceLayer.DTOs.Payment.Request;

public class CreatePaymentRequest
{
    public int? OrderId { get; set; }

    public decimal? Amount { get; set; }

    public string? PaymentMethod { get; set; }
}
