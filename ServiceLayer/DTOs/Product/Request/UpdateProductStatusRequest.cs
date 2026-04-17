using System.ComponentModel.DataAnnotations;

namespace ServiceLayer.DTOs.Product.Request;

public class UpdateProductStatusRequest
{
    [Required]
    public bool? IsActive { get; set; }
}
