namespace ServiceLayer.Contracts.Prescription;

public interface IPrescriptionPricingService
{
    PrescriptionPriceCalculation Calculate(
        decimal framePrice,
        decimal lensBasePrice,
        string? lensMaterial,
        IReadOnlyCollection<string>? coatings,
        int quantity,
        string errorCode,
        string errorMessage);
}
