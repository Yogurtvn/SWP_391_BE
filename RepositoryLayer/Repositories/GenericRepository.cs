using Microsoft.EntityFrameworkCore;
using RepositoryLayer.Common;
using RepositoryLayer.Data;
using RepositoryLayer.Interfaces;
using System.Linq.Expressions;

namespace RepositoryLayer.Repositories;

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

    private IQueryable<T> BuildQuery(
        Expression<Func<T, bool>>? filter,
        string includeProperties,
        bool tracked)
    {
        IQueryable<T> query = DbSet;

        if (!tracked)
        {
            query = query.AsNoTracking();
        }

        if (filter is not null)
        {
            query = query.Where(filter);
        }

        foreach (var includeProperty in includeProperties.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            query = query.Include(includeProperty.Trim());
        }

        if (!string.IsNullOrWhiteSpace(includeProperties) && Context.Database.IsRelational())
        {
            query = query.AsSplitQuery();
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
