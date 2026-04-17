namespace ServiceLayer.DTOs.ProductImage.Response;

public class ProductImageResponse
{
    public int ImageId { get; set; }

    public string ImageUrl { get; set; } = string.Empty;

    public int DisplayOrder { get; set; }

    public bool IsPrimary { get; set; }
}
