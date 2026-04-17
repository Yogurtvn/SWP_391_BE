namespace ServiceLayer.DTOs.Cart.Request;

public class UpsertPrescriptionCartItemRequest
{
    public int? VariantId { get; set; }

    public int? Quantity { get; set; }

    public int? LensTypeId { get; set; }

    public string? LensMaterial { get; set; }

    public List<string>? Coatings { get; set; }

    public PrescriptionEyeRequest? RightEye { get; set; }

    public PrescriptionEyeRequest? LeftEye { get; set; }

    public decimal? Pd { get; set; }

    public string? Notes { get; set; }

    public string? PrescriptionImageUrl { get; set; }
}
