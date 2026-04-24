namespace ServiceLayer.DTOs.Product.Response;

using ServiceLayer.DTOs.ProductVariant.Response;

public class ProductListItemResponse
{
    public int ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public string ProductType { get; set; } = string.Empty;

    public decimal BasePrice { get; set; }

    public string? ThumbnailUrl { get; set; }

    public bool IsActive { get; set; }

    public bool IsAvailable { get; set; }

    public bool IsReadyAvailable { get; set; }

    public bool IsPreOrderAllowed { get; set; }

    public IReadOnlyList<ProductVariantListItemResponse> Variants { get; set; } = [];
}
