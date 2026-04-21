namespace ServiceLayer.Contracts.Prescription;

public sealed class PrescriptionPriceCalculation
{
    public string? LensMaterial { get; init; }

    public IReadOnlyList<string> Coatings { get; init; } = [];

    public decimal FramePrice { get; init; }

    public decimal LensBasePrice { get; init; }

    public decimal MaterialPrice { get; init; }

    public decimal CoatingPrice { get; init; }

    public decimal LensPrice { get; init; }

    public decimal TotalPrice { get; init; }
}
