namespace ServiceLayer.DTOs.Cart.Response;

public class PrescriptionCartItemDetailResponse
{
    public int LensTypeId { get; set; }

    public string? LensCode { get; set; }

    public string? LensName { get; set; }

    public string? LensMaterial { get; set; }

    public List<string> Coatings { get; set; } = [];

    public decimal LensBasePrice { get; set; }

    public decimal MaterialPrice { get; set; }

    public decimal CoatingPrice { get; set; }

    public decimal LensPrice { get; set; }

    public PrescriptionEyeResponse RightEye { get; set; } = new();

    public PrescriptionEyeResponse LeftEye { get; set; } = new();

    public decimal Pd { get; set; }

    public string? Notes { get; set; }

    public string? PrescriptionImageUrl { get; set; }
}
