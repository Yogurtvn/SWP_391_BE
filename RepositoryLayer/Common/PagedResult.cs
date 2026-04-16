namespace RepositoryLayer.Common;

public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = [];

    public int Page { get; init; }

    public int PageSize { get; init; }

    public int TotalItems { get; init; }

    public int TotalPages { get; init; }

    public bool HasPreviousPage => Page > PaginationRequest.DefaultPage;

    public bool HasNextPage => Page < TotalPages;

    public PagedResult()
    {
    }

    public PagedResult(IReadOnlyList<T> items, int page, int pageSize, int totalItems)
    {
        var safePage = page < PaginationRequest.DefaultPage ? PaginationRequest.DefaultPage : page;
        var safePageSize = pageSize < 1
            ? PaginationRequest.DefaultPageSize
            : Math.Min(pageSize, PaginationRequest.MaxPageSize);
        var safeTotalItems = Math.Max(totalItems, 0);

        Items = items;
        Page = safePage;
        PageSize = safePageSize;
        TotalItems = safeTotalItems;
        TotalPages = safeTotalItems == 0
            ? 0
            : (int)Math.Ceiling(safeTotalItems / (double)safePageSize);
    }

    public static PagedResult<T> Create(IReadOnlyList<T> items, int page, int pageSize, int totalItems)
    {
        return new PagedResult<T>(items, page, pageSize, totalItems);
    }
}
