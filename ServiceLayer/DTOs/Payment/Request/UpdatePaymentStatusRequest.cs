namespace ServiceLayer.DTOs.Payment.Request;

public class UpdatePaymentStatusRequest
{
    public string? PaymentStatus { get; set; }

    public string? TransactionCode { get; set; }

    public string? Notes { get; set; }
}
