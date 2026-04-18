namespace ServiceLayer.DTOs.CatalogSupport.Request;

public class CalculatePrescriptionPricingRequest
{
    public int? VariantId { get; set; }

    public int? LensTypeId { get; set; }

    public List<string>? Coatings { get; set; }

    public int? Quantity { get; set; }
}
