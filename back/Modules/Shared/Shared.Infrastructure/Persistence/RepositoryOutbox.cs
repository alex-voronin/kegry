using Microsoft.EntityFrameworkCore;
using SharedCore.Interfaces;
using Wolverine.EntityFrameworkCore;

namespace Shared.Infrastructure.Persistence;

public class RepositoryOutbox<TContext, TEntity> : Repository<TContext, TEntity>, IRepositoryOutbox<TEntity>
    where TEntity : class
    where TContext : DbContext
{
    private readonly IDbContextOutbox<TContext> _contextOutbox;

    public RepositoryOutbox(IDbContextOutbox<TContext> contextOutbox) : base(contextOutbox.DbContext)
    {
        _contextOutbox = contextOutbox;
    }

    public async Task PublishEventAsync<T>(T @event, CancellationToken ct = default)
    {
        await _contextOutbox.PublishAsync(@event);
    }

    public async Task SaveChangesAndFlushMessagesAsync(CancellationToken ct = default)
    {
        await _contextOutbox.SaveChangesAndFlushMessagesAsync(ct);
    }
}