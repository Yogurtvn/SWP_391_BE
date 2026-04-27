using Microsoft.EntityFrameworkCore;
using RepositoryLayer.Common;
using RepositoryLayer.Data;
using RepositoryLayer.Interfaces;
using System.Linq.Expressions;

namespace RepositoryLayer.Repositories;

/// <summary>
/// Triển khai cụ thể của Generic Repository bằng Entity Framework Core.
/// Giúp tái sử dụng các thao tác CRUD cơ bản cho mọi Entity trong Database.
/// </summary>
public class GenericRepository<T>(OnlineEyewearDbContext context) : IGenericRepository<T>
    where T : class
{
    protected readonly OnlineEyewearDbContext Context = context;
    protected readonly DbSet<T> DbSet = context.Set<T>();

    public async Task<T?> GetByIdAsync(object id)
    {
        return await DbSet.FindAsync(id);
    }

    public async Task<IEnumerable<T>> GetAllAsync()
    {
        return await DbSet.ToListAsync();
    }

    public async Task<IEnumerable<T>> FindAsync(
        Expression<Func<T, bool>>? filter = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        string includeProperties = "",
        bool tracked = true)
    {
        IQueryable<T> query = ApplyOrdering(BuildQuery(filter, includeProperties, tracked), orderBy);
        return await query.ToListAsync();
    }

    public async Task<PagedResult<T>> GetPagedAsync(
        PaginationRequest paginationRequest,
        Expression<Func<T, bool>>? filter = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        string includeProperties = "",
        bool tracked = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(paginationRequest);

        var totalItems = await BuildQuery(filter, string.Empty, tracked: false)
            .CountAsync(cancellationToken);

        IQueryable<T> query = ApplyOrdering(BuildQuery(filter, includeProperties, tracked), orderBy);

        var items = await query
            .Skip((paginationRequest.Page - 1) * paginationRequest.PageSize)
            .Take(paginationRequest.PageSize)
            .ToListAsync(cancellationToken);

        return PagedResult<T>.Create(items, paginationRequest.Page, paginationRequest.PageSize, totalItems);
    }

    public async Task<T?> GetFirstOrDefaultAsync(
        Expression<Func<T, bool>> filter,
        string includeProperties = "",
        bool tracked = true)
    {
        return await BuildQuery(filter, includeProperties, tracked).FirstOrDefaultAsync();
    }

    public async Task AddAsync(T entity)
    {
        await DbSet.AddAsync(entity);
    }

    public async Task AddRangeAsync(IEnumerable<T> entities)
    {
        await DbSet.AddRangeAsync(entities);
    }

    public void Update(T entity)
    {
        DbSet.Update(entity);
    }

    public void Remove(T entity)
    {
        DbSet.Remove(entity);
    }

    public void RemoveRange(IEnumerable<T> entities)
    {
        DbSet.RemoveRange(entities);
    }

    public async Task<bool> ExistsAsync(Expression<Func<T, bool>> filter)
    {
        return await DbSet.AnyAsync(filter);
    }

    public async Task<int> CountAsync(Expression<Func<T, bool>>? filter = null)
    {
        return await BuildQuery(filter, string.Empty, tracked: false).CountAsync();
    }

    /// <summary>
    /// Xây dựng câu truy vấn dựa trên các điều kiện lọc, include properties và tracking.
    /// </summary>
    private IQueryable<T> BuildQuery(
        Expression<Func<T, bool>>? filter,
        string includeProperties,
        bool tracked)
    {
        IQueryable<T> query = DbSet;

        if (!tracked)
        {
            query = query.AsNoTracking(); // Không theo dõi thay đổi để tăng hiệu năng (Read-only)
        }

        if (filter is not null)
        {
            query = query.Where(filter); // Lọc dữ liệu theo điều kiện
        }

        // Tự động Include các bảng liên quan (Join)
        foreach (var includeProperty in includeProperties.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            query = query.Include(includeProperty.Trim());
        }

        if (!string.IsNullOrWhiteSpace(includeProperties) && Context.Database.IsRelational())
        {
            query = query.AsSplitQuery(); // Sử dụng Split Query để tránh bùng nổ dữ liệu khi Join nhiều bảng
        }

        return query;
    }

    private static IQueryable<T> ApplyOrdering(
        IQueryable<T> query,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy)
    {
        return orderBy is null ? query : orderBy(query);
    }
}
