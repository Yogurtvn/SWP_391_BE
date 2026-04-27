using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using RepositoryLayer.Data;
using RepositoryLayer.Interfaces;
using RepositoryLayer.Repositories;

namespace RepositoryLayer.UnitOfWork;

/// <summary>
/// Triển khai Unit of Work pattern.
/// Đảm bảo tất cả các thay đổi trên nhiều repository được thực hiện trong cùng một transaction (nguyên tử).
/// </summary>
public class UnitOfWork(OnlineEyewearDbContext context) : IUnitOfWork
{
    private readonly OnlineEyewearDbContext _context = context;
    private readonly Dictionary<Type, object> _repositories = new();
    private IDbContextTransaction? _transaction;

    /// <summary>
    /// Lấy hoặc tạo mới một Repository cho thực thể loại T.
    /// Sử dụng cơ chế lưu trữ nội bộ (Dictionary) để đảm bảo mỗi loại thực thể chỉ có một repository duy nhất.
    /// Pattern: Repository Factory - giúp quản lý việc khởi tạo repository một cách tập trung.
    /// </summary>
    public IGenericRepository<T> Repository<T>() where T : class
    {
        var entityType = typeof(T);

        // Kiểm tra xem Repository cho Entity này đã được tạo chưa
        if (!_repositories.TryGetValue(entityType, out var repository))
        {
            // Nếu chưa, khởi tạo mới và lưu vào Dictionary để dùng lại cho các lần sau
            repository = new GenericRepository<T>(_context);
            _repositories[entityType] = repository;
        }

        return (IGenericRepository<T>)repository;
    }

    /// <summary>
    /// Lưu tất cả thay đổi vào database.
    /// </summary>
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Thực hiện trừ tồn kho một cách an toàn (Atomic update) để tránh lỗi tranh chấp (Race condition).
    /// Sử dụng ExecuteUpdateAsync để cập nhật trực tiếp vào DB mà không cần load Entity lên bộ nhớ.
    /// Điều kiện `inventory.Quantity >= requestedQuantity` đảm bảo không bao giờ bị âm kho.
    /// </summary>
    public async Task<bool> TryDeductInventoryAsync(int variantId, int requestedQuantity, CancellationToken cancellationToken = default)
    {
        if (requestedQuantity <= 0)
        {
            return false;
        }

        var affectedRows = await _context.Inventory
            .Where(inventory => inventory.VariantId == variantId && inventory.Quantity >= requestedQuantity)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(
                    inventory => inventory.Quantity,
                    inventory => inventory.Quantity - requestedQuantity),
                cancellationToken);

        // Nếu affectedRows == 1 nghĩa là đã trừ kho thành công
        return affectedRows == 1;
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is not null)
        {
            return;
        }

        _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is null)
        {
            return;
        }

        try
        {
            await _transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await RollbackTransactionAsync(cancellationToken);
            throw;
        }
        finally
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is null)
        {
            return;
        }

        try
        {
            await _transaction.RollbackAsync(cancellationToken);
        }
        finally
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        GC.SuppressFinalize(this);
    }
}
