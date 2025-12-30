using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Shared.Infrastructure.Persistence;
using UserProfileModule.Domain.Aggregates;

namespace UserProfileModule.Infrastructure.Persistence.Repositories;

internal class EfCoreHobbyRepository : CachingRepository<UserProfileContext, Hobby>, IHobbyRepository
{
    public EfCoreHobbyRepository(UserProfileContext context, HybridCache cache) : base(context, cache)
    {
    }

    public async Task<Hobby[]> GetAll(CancellationToken ct = default)
    {
        return await Cache.GetOrCreateAsync(
            key: "hobby-list",
            factory: async cancel =>
            {
                return await Context.Hobbies
                    .AsNoTracking()
                    .OrderBy(h => h.Name)
                    .ToArrayAsync(cancel);
            },
            options: CachingOptions.Forever,
            cancellationToken: ct
        );
    }
}