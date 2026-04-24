namespace ServiceLayer.DTOs.CatalogSupport.Response;

public class PrescriptionOptionsResponse
{
    public IReadOnlyList<PrescriptionPricingOptionResponse> LensMaterials { get; set; } = [];

    public IReadOnlyList<PrescriptionPricingOptionResponse> Coatings { get; set; } = [];
}

public class PrescriptionPricingOptionResponse
{
    public string Code { get; set; } = string.Empty;

    public string Label { get; set; } = string.Empty;

    public decimal PriceAdjustment { get; set; }
}
