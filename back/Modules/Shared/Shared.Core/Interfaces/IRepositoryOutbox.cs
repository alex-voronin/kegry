namespace SharedCore.Interfaces;

public interface IRepositoryOutbox<TEntity> : IRepository<TEntity> where TEntity : class
{
    public Task PublishEventAsync<T>(T @event, CancellationToken ct = default);
    public Task SaveChangesAndFlushMessagesAsync(CancellationToken ct = default);
}