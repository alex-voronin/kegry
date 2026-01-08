using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using SharedCore.Interfaces;

namespace Shared.Infrastructure.Persistence;

public abstract class RepositoryBase
{
    internal abstract DbContext GetContext();
}

public class Repository<TContext, TEntity> : RepositoryBase, IRepository<TEntity>
    where TEntity : class
    where TContext : DbContext
{
    protected readonly TContext Context;

    public Repository(TContext context)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public virtual async Task<TEntity?> GetByIdAsync<TId>(
        TId id,
        bool asNoTracking = false,
        Expression<Func<TEntity, object?>>[]? includes = null,
        CancellationToken ct = default) where TId : IEquatable<TId>
    {
        IQueryable<TEntity> query = Context.Set<TEntity>();

        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        if (includes != null)
        {
            foreach (var include in includes)
            {
                query = query.Include(include);
            }
        }

        var primaryKeyName = Context.Model.FindEntityType(typeof(TEntity))!
            .FindPrimaryKey()!.Properties.Select(x => x.Name).Single();
        return await query.FirstOrDefaultAsync(e => EF.Property<TId>(e, primaryKeyName).Equals(id), ct);
    }

    public void Add(TEntity entity)
    {
        Context.Set<TEntity>().Add(entity);
    }

    public Task AddAsync(TEntity entity, CancellationToken ct = default)
    {
        return Context.Set<TEntity>().AddAsync(entity, ct).AsTask();
    }

    public void Update(TEntity entity)
    {
        Context.Set<TEntity>().Update(entity);
    }

    public void Delete(TEntity entity)
    {
        Context.Set<TEntity>().Remove(entity);
    }

    public int Count()
    {
        return Context.Set<TEntity>().Count();
    }

    public Task<int> CountAsync(CancellationToken ct = default)
    {
        return Context.Set<TEntity>().CountAsync(ct);
    }

    public void SaveChanges()
    {
        Context.SaveChanges();
    }

    public Task SaveChangesAsync(CancellationToken ct = default)
    {
        return Context.SaveChangesAsync(ct);
    }

    public Task SaveAsync(TEntity entity, CancellationToken ct)
    {
        var entry = Context.Entry(entity);
        switch (entry.State)
        {
            case EntityState.Detached:
                Context.Set<TEntity>().Add(entity);
                break;
            case EntityState.Unchanged:
                Context.Set<TEntity>().Update(entity);
                break;
        }

        return Context.SaveChangesAsync(ct);
    }

    public IDbContextCollection And(IRepository otherRepository)
    {
        return new DbContextCollection(this);
    }

    internal override DbContext GetContext()
    {
        return Context;
    }
}