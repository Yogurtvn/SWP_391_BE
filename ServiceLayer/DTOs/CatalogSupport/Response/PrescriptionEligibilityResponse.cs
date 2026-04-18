namespace ServiceLayer.DTOs.CatalogSupport.Response;

public class PrescriptionEligibilityResponse
{
    public int ProductId { get; set; }

    public bool IsEligible { get; set; }

    public string? Reason { get; set; }
}
