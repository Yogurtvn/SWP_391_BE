namespace ServiceLayer.DTOs.Prescription.Request;

public class GetPrescriptionsRequest
{
    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 20;

    public string? PrescriptionStatus { get; set; }

    public int? UserId { get; set; }

    public DateTime? FromDate { get; set; }

    public DateTime? ToDate { get; set; }

    public string? SortBy { get; set; }

    public string? SortOrder { get; set; }
}
