using System.ComponentModel.DataAnnotations;

namespace ServiceLayer.DTOs.ProductImage.Request;

public class UpdateProductImageMetadataRequest
{
    public bool? IsPrimary { get; set; }

    [Range(1, int.MaxValue)]
    public int? DisplayOrder { get; set; }
}
