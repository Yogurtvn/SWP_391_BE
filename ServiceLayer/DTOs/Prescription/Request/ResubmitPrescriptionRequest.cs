namespace ServiceLayer.DTOs.Prescription.Request;

public class ResubmitPrescriptionRequest
{
    public PrescriptionEyeInputRequest? RightEye { get; set; }

    public PrescriptionEyeInputRequest? LeftEye { get; set; }

    public decimal? Pd { get; set; }

    public string? Notes { get; set; }

    public string? PrescriptionImageUrl { get; set; }
}

public class PrescriptionEyeInputRequest
{
    public decimal? Sph { get; set; }

    public decimal? Cyl { get; set; }

    public int? Axis { get; set; }
}
