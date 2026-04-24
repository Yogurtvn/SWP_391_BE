namespace ServiceLayer.DTOs.Orders;

public class OrderItemPrescriptionResponse
{
    public int PrescriptionId { get; set; }

    public int LensTypeId { get; set; }

    public string? LensTypeCode { get; set; }

    public string? LensMaterial { get; set; }

    public List<string> Coatings { get; set; } = [];

    public decimal LensBasePrice { get; set; }

    public decimal MaterialPrice { get; set; }

    public decimal CoatingPrice { get; set; }

    public decimal TotalLensPrice { get; set; }

    public OrderPrescriptionEyeResponse RightEye { get; set; } = new();

    public OrderPrescriptionEyeResponse LeftEye { get; set; } = new();

    public decimal Pd { get; set; }

    public string? PrescriptionImageUrl { get; set; }

    public string PrescriptionStatus { get; set; } = string.Empty;

    public int? StaffId { get; set; }

    public DateTime? VerifiedAt { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }
}

public class OrderPrescriptionEyeResponse
{
    public decimal Sph { get; set; }

    public decimal Cyl { get; set; }

    public int Axis { get; set; }
}
