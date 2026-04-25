using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using RepositoryLayer.Data;
using RepositoryLayer.Interfaces;
using RepositoryLayer.Repositories;

namespace RepositoryLayer.UnitOfWork;

public class UnitOfWork(OnlineEyewearDbContext context) : IUnitOfWork
{
    private readonly OnlineEyewearDbContext _context = context;
    private readonly Dictionary<Type, object> _repositories = new();
    private IDbContextTransaction? _transaction;

    public IGenericRepository<T> Repository<T>() where T : class
    {
        var entityType = typeof(T);

        if (!_repositories.TryGetValue(entityType, out var repository))
        {
            repository = new GenericRepository<T>(_context);
            _repositories[entityType] = repository;
        }

        return (IGenericRepository<T>)repository;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }

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
