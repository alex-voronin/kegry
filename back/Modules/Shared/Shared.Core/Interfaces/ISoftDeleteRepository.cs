namespace SharedCore.Interfaces;

public interface ISoftDeleteRepository<TEntity, TId>
    where TEntity : class, ISoftDeletable<TId>
{
    Task SoftDeleteAsync(TId id, CancellationToken ct = default);
}