namespace SharedCore.Interfaces;

public interface IRepositoryOutbox<TEntity> : IRepository<TEntity> where TEntity : class
{
    public ValueTask PublishEventAsync<T>(T @event);
    public ValueTask ScheduleEventAsync<T>(T @event, TimeSpan delay);
    public Task SaveChangesAndFlushMessagesAsync(CancellationToken ct = default);
    public Task FlushOutgoingMessagesAsync();
}