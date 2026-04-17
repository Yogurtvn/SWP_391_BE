using System.ComponentModel.DataAnnotations;

namespace ServiceLayer.DTOs.Product.Request;

public class UpdateProductRequest
{
    [Required]
    [MaxLength(255)]
    public string ProductName { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int CategoryId { get; set; }

    [Required]
    [MaxLength(50)]
    public string ProductType { get; set; } = string.Empty;

    public bool PrescriptionCompatible { get; set; }

    public string? Description { get; set; }

    [Range(typeof(decimal), "0", "79228162514264337593543950335")]
    public decimal BasePrice { get; set; }
}
