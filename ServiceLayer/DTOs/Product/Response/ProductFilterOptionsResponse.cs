namespace ServiceLayer.DTOs.Product.Response;

public class ProductFilterOptionsResponse
{
    public IReadOnlyList<string> Colors { get; set; } = [];

    public IReadOnlyList<string> Sizes { get; set; } = [];

    public IReadOnlyList<string> FrameTypes { get; set; } = [];
}
