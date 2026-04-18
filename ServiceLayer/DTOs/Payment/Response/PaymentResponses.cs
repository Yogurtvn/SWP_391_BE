namespace ServiceLayer.DTOs.Payment.Response;

public class CreatePaymentResponse
{
    public int PaymentId { get; set; }

    public int OrderId { get; set; }

    public string PaymentMethod { get; set; } = string.Empty;

    public string PaymentStatus { get; set; } = string.Empty;

    public string? PayUrl { get; set; }

    public string? Deeplink { get; set; }

    public string? QrCodeUrl { get; set; }
}

public class PaymentListItemResponse
{
    public int PaymentId { get; set; }

    public int OrderId { get; set; }

    public string PaymentMethod { get; set; } = string.Empty;

    public string PaymentStatus { get; set; } = string.Empty;
}

public class PaymentDetailResponse
{
    public int PaymentId { get; set; }

    public int OrderId { get; set; }

    public decimal Amount { get; set; }

    public string PaymentMethod { get; set; } = string.Empty;

    public string PaymentStatus { get; set; } = string.Empty;

    public DateTime? PaidAt { get; set; }
}

public class PaymentStatusUpdatedResponse
{
    public string Message { get; set; } = string.Empty;

    public string PaymentStatus { get; set; } = string.Empty;
}

public class PaymentHistoriesResponse
{
    public List<PaymentHistoryListItemResponse> Items { get; set; } = [];
}

public class PaymentHistoryListItemResponse
{
    public int PaymentHistoryId { get; set; }

    public string PaymentStatus { get; set; } = string.Empty;

    public string? TransactionCode { get; set; }

    public DateTime CreatedAt { get; set; }
}

public class PaymentActionResponse
{
    public int PaymentId { get; set; }

    public string PaymentStatus { get; set; } = string.Empty;

    public string? PayUrl { get; set; }

    public string? Deeplink { get; set; }

    public string? QrCodeUrl { get; set; }
}

public class MomoCreateGatewayResultDto
{
    public bool IsSuccessStatusCode { get; set; }

    public int HttpStatusCode { get; set; }

    public string RawResponse { get; set; } = string.Empty;

    public int ResultCode { get; set; }

    public string Message { get; set; } = string.Empty;

    public string? PayUrl { get; set; }

    public string? Deeplink { get; set; }

    public string? QrCodeUrl { get; set; }
}
