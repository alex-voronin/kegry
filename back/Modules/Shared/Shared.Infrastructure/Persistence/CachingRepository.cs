using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

namespace Shared.Infrastructure.Persistence;

public abstract class CachingRepository<TContext, TEntity> : Repository<TContext, TEntity>
    where TEntity : class
    where TContext : DbContext
{
    protected readonly HybridCache Cache;

    protected CachingRepository(TContext context, HybridCache cache) : base(context)
    {
        Cache = cache;
    }
}