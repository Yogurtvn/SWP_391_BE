namespace ServiceLayer.DTOs.ProductVariant.Response;

public class ProductVariantDetailResponse
{
    public int VariantId { get; set; }

    public string Sku { get; set; } = string.Empty;

    public string? Color { get; set; }

    public string? Size { get; set; }

    public string? FrameType { get; set; }

    public decimal Price { get; set; }

    public int Quantity { get; set; }

    public bool IsPreOrderAllowed { get; set; }

    public DateTime? ExpectedRestockDate { get; set; }
}
