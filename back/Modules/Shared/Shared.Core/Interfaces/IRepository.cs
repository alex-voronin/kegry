using System.Linq.Expressions;

namespace SharedCore.Interfaces;


public interface IRepository
{
    public int Count();
    public Task<int> CountAsync(CancellationToken ct = default);
    public void SaveChanges();
    public Task SaveChangesAsync(CancellationToken ct = default);

    public IDbContextCollection And(IRepository otherRepository);
}

public interface IRepository<TEntity> : IRepository where TEntity : class
{
    public Task<TEntity?> GetByIdAsync<TId>(
        TId id,
        bool asNoTracking = false,
        Expression<Func<TEntity, object?>>[]? includes = null,
        CancellationToken ct = default ) where TId: IEquatable<TId>;
    public void Attach(TEntity entity);
    public void Add(TEntity entity);
    public Task AddAsync(TEntity entity, CancellationToken ct = default);
    public void Update(TEntity entity);
    void Delete(TEntity entity);
    Task SaveAsync(TEntity entity, CancellationToken ct);
}