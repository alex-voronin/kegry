using Microsoft.EntityFrameworkCore;
using SharedCore.Interfaces;
using Wolverine;
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

    public ValueTask PublishEventAsync<T>(T @event)
    {
        return _contextOutbox.PublishAsync(@event);
    }

    public ValueTask ScheduleEventAsync<T>(T @event, TimeSpan delay)
    {
        return _contextOutbox.ScheduleAsync(@event, delay);
    }

    public async Task SaveChangesAndFlushMessagesAsync(CancellationToken ct = default)
    {
        await _contextOutbox.SaveChangesAndFlushMessagesAsync(ct);
    }

    public async Task FlushOutgoingMessagesAsync()
    {
        await _contextOutbox.FlushOutgoingMessagesAsync();
    }
}