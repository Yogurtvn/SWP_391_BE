namespace ServiceLayer.DTOs.Prescription.Response;

public class PrescriptionListItemResponse
{
    public int PrescriptionId { get; set; }

    public int? OrderId { get; set; }

    public string PrescriptionStatus { get; set; } = string.Empty;
}

public class PrescriptionDetailResponse
{
    public int PrescriptionId { get; set; }

    public PrescriptionEyeResponse RightEye { get; set; } = new();

    public PrescriptionEyeResponse LeftEye { get; set; } = new();

    public decimal Pd { get; set; }

    public string? PrescriptionImageUrl { get; set; }
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
