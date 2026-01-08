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

    public ValueTask<Hobby[]> GetAll(CancellationToken ct = default)
    {
        return Cache.GetOrCreateAsync(
            key: "hobby-list",
            factory: async cancel =>
            {
                return await Context.Hobbies
                    .AsNoTracking()
                    .OrderBy(h => h.Name)
                    .ToArrayAsync(cancel);
            },
            options: CachingOptions.Week,
            cancellationToken: ct
        );
    }

    public async ValueTask<IReadOnlyCollection<Guid>> GetNonExistingIds(Guid[] hobbyIds, CancellationToken ct = default)
    {
        if (hobbyIds.Length == 0)
        {
            return [];
        }
        var allHobbies = await GetAll(ct); // it's cheap because of caching
        var existingKeySet = allHobbies.Select(x => x.Id).ToHashSet();
        var validatedKeySet = hobbyIds.ToHashSet();
        validatedKeySet.ExceptWith(existingKeySet);
        return validatedKeySet;
    }
}