using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SharedCore.Interfaces;

namespace Shared.Infrastructure.Persistence;

public class MultiRepositoryTransaction : IMultiRepositoryTransaction
{
    private readonly IDbContextCollection _dbContextCollection;
    private readonly IDbContextTransaction _transaction;
    private bool _isSaved;

    internal MultiRepositoryTransaction(IDbContextCollection dbContextCollection, IDbContextTransaction transaction)
    {
        _dbContextCollection = dbContextCollection;
        _transaction = transaction;
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await _dbContextCollection.SaveChangesAsync(ct);
        _isSaved = true;
    }

    public async Task CommitAsync(CancellationToken ct = default)
    {
        if (!_isSaved)
        {
            await SaveChangesAsync(ct);
        }
        await _transaction.CommitAsync(ct);
    }
}

internal class DbContextCollection : IDbContextCollection
{
    private readonly HashSet<DbContext> _contexts = new HashSet<DbContext>();

    internal DbContextCollection(RepositoryBase repositoryBase)
    {
        _contexts.Add(repositoryBase.GetContext());
    }

    public IDbContextCollection And(IRepository repository)
    {
        if (repository is RepositoryBase repositoryBase)
        {
            _contexts.Add(repositoryBase.GetContext());
        }
        else
        {
            throw new InvalidOperationException("Unknown repository type");
        }

        return this;
    }

    public async Task<IMultiRepositoryTransaction> StartTransactionAsync(CancellationToken ct = default)
    {
        var tx = await BeginTransaction(_contexts.First(), ct);
        foreach (var context in _contexts)
        {
            await ApplyTransaction(context, tx, ct);
        }
        return new MultiRepositoryTransaction(this, tx);
    }

    public void SaveChanges()
    {
        foreach (var context in _contexts)
        {
            context.SaveChanges();
        }
    }

    public async Task SaveChangesAsync(CancellationToken ct)
    {
        foreach (var context in _contexts)
        {
            await context.SaveChangesAsync(ct);
        }
    }

    protected virtual async Task<IDbContextTransaction> BeginTransaction(DbContext dbContext, CancellationToken ct)
    {
        return await _contexts.First().Database.BeginTransactionAsync(ct);
    }

    protected virtual async Task ApplyTransaction(
        DbContext context,
        IDbContextTransaction transaction,
        CancellationToken ct = default)
    {
        await context.Database.UseTransactionAsync(transaction.GetDbTransaction(), ct);
    }
}