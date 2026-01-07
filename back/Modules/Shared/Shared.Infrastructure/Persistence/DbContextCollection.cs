
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SharedCore.Interfaces;

namespace Shared.Infrastructure.Persistence;

public class MultiRepositoryTransaction : IMultiRepositoryTransaction
{
    private readonly DbContextCollection _dbContextCollection;
    private readonly IDbContextTransaction _transaction;
    private bool _isSaved;

    internal MultiRepositoryTransaction(DbContextCollection dbContextCollection, IDbContextTransaction transaction)
    {
        _dbContextCollection = dbContextCollection;
        _transaction = transaction;
    }

    public async Task SaveChangesAsync(CancellationToken ct)
    {
        await _dbContextCollection.SaveChangesAsync(ct);
        _isSaved = true;
    }

    public async Task CommitAsync(CancellationToken ct)
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
        var tx = await _contexts.First().Database.BeginTransactionAsync();
        foreach (var context in _contexts)
        {
            await context.Database.UseTransactionAsync(tx.GetDbTransaction(), ct);
        }
        return new MultiRepositoryTransaction(this, tx);
    }

    public void SaveChanges()
    {
        SaveDbContexts(_contexts);
    }

    public async Task SaveChangesAsync(CancellationToken ct)
    {
        await SaveDbContextsAsync(_contexts, ct);
    }

    private static void SaveDbContexts(IEnumerable<DbContext> contexts)
    {
        foreach (var context in contexts)
        {
            context.SaveChanges();
        }
    }

    private static async Task SaveDbContextsAsync(IEnumerable<DbContext> contexts, CancellationToken ct = default)
    {
        foreach (var context in contexts)
        {
            await context.SaveChangesAsync(ct);
        }
    }
}