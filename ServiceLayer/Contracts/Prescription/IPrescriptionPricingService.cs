namespace ServiceLayer.Contracts.Prescription;

public interface IPrescriptionPricingService
{
    PrescriptionPriceCalculation Calculate(
        decimal framePrice,
        decimal lensBasePrice,
        int quantity,
        string errorCode,
        string errorMessage);
}
