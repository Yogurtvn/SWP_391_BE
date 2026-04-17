namespace ServiceLayer.DTOs.ProductVariant.Request;

public class GetProductVariantsRequest
{
    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 20;

    public string? Color { get; set; }

    public string? Size { get; set; }

    public string? FrameType { get; set; }

    public bool? IsActive { get; set; }

    public string? SortBy { get; set; }

    public string? SortOrder { get; set; }
}
