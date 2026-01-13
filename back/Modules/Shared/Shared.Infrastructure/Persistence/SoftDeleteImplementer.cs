using Microsoft.EntityFrameworkCore;
using SharedCore.Interfaces;
using SharedCore.Services.LifeTimes;

namespace Shared.Infrastructure.Persistence;

public interface ISoftDeleteImplementer<TEntity, TId>
    where TEntity : class, ISoftDeletable<TId>
{
    Task SoftDeleteAsync(TId id, CancellationToken ct = default);
}

public class SoftDeleteImplementer<TContext, TEntity, TId>
    : ISoftDeleteImplementer<TEntity, TId>, IScoped
    where TEntity : class, ISoftDeletable<TId>
    where TContext : DbContext
{
    private readonly TContext _context;

    public SoftDeleteImplementer(TContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public Task SoftDeleteAsync(TId id, CancellationToken ct = default)
    {
        return _context
            .Set<TEntity>()
            .Where(x => x.Id.Equals(id))
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(x => x.IsDeleted, true),
                ct);
    }
}