using ServiceLayer.DTOs.ProductImage.Response;
using ServiceLayer.DTOs.ProductVariant.Response;

namespace ServiceLayer.DTOs.Product.Response;

public class ProductDetailResponse
{
    public int ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public int? CategoryId { get; set; }

    public string ProductType { get; set; } = string.Empty;

    public string? Description { get; set; }

    public decimal BasePrice { get; set; }

    public bool IsActive { get; set; }

    public bool PrescriptionCompatible { get; set; }

    public IReadOnlyList<ProductVariantListItemResponse> Variants { get; set; } = [];

    public IReadOnlyList<ProductImageResponse> Images { get; set; } = [];
}
