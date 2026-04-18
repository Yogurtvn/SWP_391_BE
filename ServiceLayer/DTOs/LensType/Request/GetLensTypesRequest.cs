namespace ServiceLayer.DTOs.LensType.Request;

public class GetLensTypesRequest
{
    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 20;

    public string? Search { get; set; }

    public bool? IsActive { get; set; }

    public string? SortBy { get; set; }

    public string? SortOrder { get; set; }
}
