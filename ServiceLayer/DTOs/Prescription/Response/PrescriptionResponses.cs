namespace ServiceLayer.DTOs.Prescription.Response;

public class PrescriptionListItemResponse
{
    public int PrescriptionId { get; set; }

    public int UserId { get; set; }

    public string? CustomerName { get; set; }

    public string? CustomerEmail { get; set; }

    public int? OrderId { get; set; }

    public int LensTypeId { get; set; }

    public string? LensTypeCode { get; set; }

    public string? LensMaterial { get; set; }

    public decimal TotalLensPrice { get; set; }

    public string? PrescriptionImageUrl { get; set; }

    public string PrescriptionStatus { get; set; } = string.Empty;

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }
}

public class PrescriptionDetailResponse
{
    public int PrescriptionId { get; set; }

    public int UserId { get; set; }

    public string? CustomerName { get; set; }

    public string? CustomerEmail { get; set; }

    public int? OrderId { get; set; }

    public int LensTypeId { get; set; }

    public string? LensTypeCode { get; set; }

    public string? LensMaterial { get; set; }

    public List<string> Coatings { get; set; } = [];

    public decimal LensBasePrice { get; set; }

    public decimal MaterialPrice { get; set; }

    public decimal CoatingPrice { get; set; }

    public decimal TotalLensPrice { get; set; }

    public PrescriptionEyeResponse RightEye { get; set; } = new();

    public PrescriptionEyeResponse LeftEye { get; set; } = new();

    public decimal Pd { get; set; }

    public string? PrescriptionImageUrl { get; set; }

    public string? PrescriptionStatus { get; set; }

    public int? StaffId { get; set; }

    public DateTime? VerifiedAt { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }
}

public class PrescriptionEyeResponse
{
    public decimal Sph { get; set; }

    public decimal Cyl { get; set; }

    public int Axis { get; set; }
}

public class PrescriptionStatusResponse
{
    public string Message { get; set; } = string.Empty;

    public string PrescriptionStatus { get; set; } = string.Empty;
}
