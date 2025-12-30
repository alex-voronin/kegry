using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using SharedCore.Interfaces;

namespace Shared.Infrastructure.Persistence;

public class Repository<TContext, TEntity> : IRepository<TEntity>
    where TEntity : class
    where TContext : DbContext
{
    protected readonly TContext Context;

    public Repository(TContext context)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public virtual async Task<TEntity?> GetByIdAsync(
        Guid id,
        bool asNoTracking = false,
        Expression<Func<TEntity, object>>[]? includes = null,
        CancellationToken ct = default)
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
        return await query.FirstOrDefaultAsync(e => EF.Property<Guid>(e, primaryKeyName) == id, ct);
    }

    public void Add(TEntity entity)
    {
        Context.Set<TEntity>().Add(entity);
    }

    public async Task AddAsync(TEntity entity, CancellationToken ct = default)
    {
        await Context.Set<TEntity>().AddAsync(entity, ct);
    }

    public void Delete(TEntity entity)
    {
        Context.Set<TEntity>().Remove(entity);
    }

    public int Count()
    {
        return Context.Set<TEntity>().Count();
    }

    public async Task<int> CountAsync(CancellationToken ct = default)
    {
        return await Context.Set<TEntity>().CountAsync(ct);
    }

    public void SaveChanges()
    {
        Context.SaveChanges();
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await Context.SaveChangesAsync(ct);
    }

    public async Task SaveAsync(TEntity entity, CancellationToken ct)
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

        await Context.SaveChangesAsync(ct);
    }
}