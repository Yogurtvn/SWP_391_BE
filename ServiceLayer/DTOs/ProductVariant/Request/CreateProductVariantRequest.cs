using System.ComponentModel.DataAnnotations;

namespace ServiceLayer.DTOs.ProductVariant.Request;

public class CreateProductVariantRequest
{
    [Required]
    [MaxLength(100)]
    public string Sku { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? FrameType { get; set; }

    [MaxLength(20)]
    public string? Size { get; set; }

    [MaxLength(50)]
    public string? Color { get; set; }

    [Range(typeof(decimal), "0", "79228162514264337593543950335")]
    public decimal Price { get; set; }

    [Range(0, int.MaxValue)]
    public int Quantity { get; set; }

    public bool IsPreOrderAllowed { get; set; }

    public DateTime? ExpectedRestockDate { get; set; }

    [MaxLength(255)]
    public string? PreOrderNote { get; set; }
}
