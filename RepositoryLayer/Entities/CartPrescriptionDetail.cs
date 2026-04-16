namespace RepositoryLayer.Entities;

public class CartPrescriptionDetail
{
    public int CartPrescriptionId { get; set; }

    public int CartItemId { get; set; }

    public int LensTypeId { get; set; }

    public string? LensTypeCode { get; set; }

    public string? LensMaterial { get; set; }

    public string? Coatings { get; set; }

    public decimal LensBasePrice { get; set; }

    public decimal CoatingPrice { get; set; }

    public decimal TotalLensPrice { get; set; }

    public decimal? SphLeft { get; set; }

    public decimal? SphRight { get; set; }

    public decimal? CylLeft { get; set; }

    public decimal? CylRight { get; set; }

    public int? AxisLeft { get; set; }

    public int? AxisRight { get; set; }

    public decimal? Pd { get; set; }

    public string? PrescriptionImage { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }

    public CartItem CartItem { get; set; } = null!;

    public LensType LensType { get; set; } = null!;
}
