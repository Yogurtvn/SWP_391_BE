namespace ServiceLayer.Contracts.Prescription;

public sealed class PrescriptionPriceCalculation
{
    public decimal FramePrice { get; init; }

    public decimal LensBasePrice { get; init; }

    public decimal LensPrice { get; init; }

    public decimal TotalPrice { get; init; }
}
