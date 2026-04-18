using System.ComponentModel.DataAnnotations;

namespace ServiceLayer.DTOs.Prescription.Request;

public class RequestMorePrescriptionInfoRequest
{
    [Required]
    [MaxLength(255)]
    public string Notes { get; set; } = string.Empty;
}
