namespace ServiceLayer.DTOs.CatalogSupport.Response;

public class VariantAvailabilityResponse
{
    public int VariantId { get; set; }

    public int Quantity { get; set; }

    public bool IsReadyAvailable { get; set; }

    public bool IsPreOrderAllowed { get; set; }

    public DateTime? ExpectedRestockDate { get; set; }
}
