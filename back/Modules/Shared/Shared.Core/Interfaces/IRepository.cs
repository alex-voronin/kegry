using System.Linq.Expressions;

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
    public Task<TEntity?> GetByIdAsync(
        Guid id,
        bool asNoTracking = false,
        Expression<Func<TEntity, object>>[]? includes = null,
        CancellationToken ct = default );
    public void Add(TEntity entity);
    public Task AddAsync(TEntity entity, CancellationToken ct = default);
    public void Update(TEntity entity);
    void Delete(TEntity entity);
    Task SaveAsync(TEntity entity, CancellationToken ct);
}