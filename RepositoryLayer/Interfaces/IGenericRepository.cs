using RepositoryLayer.Common;
using System.Linq.Expressions;

namespace RepositoryLayer.Interfaces;

public interface IGenericRepository<T> where T : class
{
    Task<T?> GetByIdAsync(object id);

    Task<IEnumerable<T>> GetAllAsync();

    Task<IEnumerable<T>> FindAsync(
        Expression<Func<T, bool>>? filter = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        string includeProperties = "",
        bool tracked = true);

    Task<PagedResult<T>> GetPagedAsync(
        PaginationRequest paginationRequest,
        Expression<Func<T, bool>>? filter = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        string includeProperties = "",
        bool tracked = false,
        CancellationToken cancellationToken = default);

    Task<T?> GetFirstOrDefaultAsync(
        Expression<Func<T, bool>> filter,
        string includeProperties = "",
        bool tracked = true);

    Task AddAsync(T entity);

    Task AddRangeAsync(IEnumerable<T> entities);

    void Update(T entity);

    void Remove(T entity);

    void RemoveRange(IEnumerable<T> entities);

    Task<bool> ExistsAsync(Expression<Func<T, bool>> filter);

    Task<int> CountAsync(Expression<Func<T, bool>>? filter = null);
}
