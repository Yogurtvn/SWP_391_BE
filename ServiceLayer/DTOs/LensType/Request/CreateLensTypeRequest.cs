using System.ComponentModel.DataAnnotations;

namespace ServiceLayer.DTOs.LensType.Request;

public class CreateLensTypeRequest
{
    [Required]
    [MaxLength(50)]
    public string LensCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string LensName { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [Range(typeof(decimal), "0", "79228162514264337593543950335")]
    public decimal Price { get; set; }

    public bool IsActive { get; set; } = true;
}
