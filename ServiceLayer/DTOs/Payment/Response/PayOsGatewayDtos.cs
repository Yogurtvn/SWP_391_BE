namespace ServiceLayer.DTOs.Payment.Response;

public class PayOsCreatePaymentLinkResult
{
    public long OrderCode { get; set; }

    public string CheckoutUrl { get; set; } = string.Empty;

    public string? QrCode { get; set; }
}

public enum PayOsWebhookVerificationStatus
{
    Valid = 1,
    InvalidSignature = 2,
    InvalidPayload = 3
}

public class PayOsWebhookVerificationResult
{
    public PayOsWebhookVerificationStatus Status { get; set; }

    public string Message { get; set; } = string.Empty;

    public PayOsVerifiedWebhookData? Data { get; set; }
}

public class PayOsVerifiedWebhookData
{
    public long OrderCode { get; set; }

    public int Amount { get; set; }

    public string? Description { get; set; }

    public string? Reference { get; set; }
}

public class PayOsPaymentLinkInformationResult
{
    public long OrderCode { get; set; }

    public string PaymentLinkId { get; set; } = string.Empty;

    public int Amount { get; set; }

    public int AmountPaid { get; set; }

    public int AmountRemaining { get; set; }

    public string Status { get; set; } = string.Empty;
}
