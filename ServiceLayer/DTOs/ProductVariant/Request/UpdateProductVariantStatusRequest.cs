using System.ComponentModel.DataAnnotations;

namespace ServiceLayer.DTOs.ProductVariant.Request;

public class UpdateProductVariantStatusRequest
{
    [Required]
    public bool? IsActive { get; set; }
}
