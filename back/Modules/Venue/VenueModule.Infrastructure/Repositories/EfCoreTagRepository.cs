using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Shared.Infrastructure.Persistence;
using VenueModule.Domain.Aggregates;
using VenueModule.Domain.Repositories;
using VenueModule.Infrastructure.Persistence;

namespace VenueModule.Infrastructure.Repositories;

public class EfCoreTagRepository : CachingRepository<VenueContext, Tag>, ITagRepository
{
    public EfCoreTagRepository(VenueContext context, HybridCache cache) : base(context, cache)
    {
    }

    public ValueTask<Tag[]> GetAllAsync(CancellationToken ct)
    {
        return Cache.GetOrCreateAsync(
            key: "venue-tag-list",
            factory: async cancel =>
            {
                return await Context.Tags
                    .AsNoTracking()
                    .OrderBy(t => t.SortOrder)
                    .ToArrayAsync(cancel);
            },
            options: CachingOptions.Week,
            cancellationToken: ct
        );
    }
}