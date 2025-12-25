namespace SharedCore.Interfaces;

public interface IRepository
{
    public int Count();
    public Task<int> CountAsync(CancellationToken ct = default);
    public void SaveChanges();
    public Task SaveChangesAsync(CancellationToken ct = default);
}

public interface IRepository<TEntity> : IRepository where TEntity : class
{
    Task<TEntity?> GetByIdAsync(long id, bool asNoTracking = false, CancellationToken ct = default);
    public void Add(TEntity entity);
    public Task AddAsync(TEntity entity, CancellationToken ct = default);
    void Delete(TEntity entity);
    Task SaveAsync(TEntity entity, CancellationToken ct);
}