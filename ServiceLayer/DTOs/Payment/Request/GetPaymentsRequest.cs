namespace ServiceLayer.DTOs.Payment.Request;

public class GetPaymentsRequest
{
    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 20;

    public string? PaymentMethod { get; set; }

    public string? PaymentStatus { get; set; }

    public int? OrderId { get; set; }

    public DateTime? FromDate { get; set; }

    public DateTime? ToDate { get; set; }

    public string? SortBy { get; set; }

    public string? SortOrder { get; set; }
}
