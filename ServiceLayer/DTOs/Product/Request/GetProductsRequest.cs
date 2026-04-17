namespace ServiceLayer.DTOs.Product.Request;

public class GetProductsRequest
{
    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 20;

    public string? Search { get; set; }

    public int? CategoryId { get; set; }

    public string? ProductType { get; set; }

    public decimal? MinPrice { get; set; }

    public decimal? MaxPrice { get; set; }

    public string? Color { get; set; }

    public string? Size { get; set; }

    public string? FrameType { get; set; }

    public bool? PrescriptionCompatible { get; set; }

    public string? SortBy { get; set; }

    public string? SortOrder { get; set; }
}
