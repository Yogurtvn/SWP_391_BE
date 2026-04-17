namespace ServiceLayer.DTOs.ProductImage.Response;

public class ProductImagesResponse
{
    public IReadOnlyList<ProductImageResponse> Items { get; set; } = [];
}
