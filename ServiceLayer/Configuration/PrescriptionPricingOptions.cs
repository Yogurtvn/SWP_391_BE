namespace ServiceLayer.Configuration;

public class PrescriptionPricingOptions
{
    public const string SectionName = "PrescriptionPricing";

    public Dictionary<string, decimal> MaterialPriceAdjustments { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public Dictionary<string, decimal> CoatingPriceAdjustments { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
