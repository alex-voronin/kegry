using LocationModule.Domain.Aggregates;
using LocationModule.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Persistence;
using Wolverine.EntityFrameworkCore;

namespace LocationModule.Infrastructure.Persistence.Repositories;

internal class EfCoreTrackedUserPairRepository : RepositoryOutbox<LocationContext, TrackedUserPair>, ITrackedUserPairRepository
{
    public EfCoreTrackedUserPairRepository(IDbContextOutbox<LocationContext> contextOutbox) : base(contextOutbox)
    {
    }

    public Task<TrackedUserPair[]> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        return Context.TrackedUsers
            .Where(tp => tp.FirstUserId == userId || tp.SecondUserId == userId)
            .ToArrayAsync(ct);
    }

    public Task DeleteByUserIds(Guid[] userIds, CancellationToken ct = default)
    {
        return Context.TrackedUsers
            .Where(tp => userIds.Contains(tp.FirstUserId) || userIds.Contains(tp.SecondUserId))
            .ExecuteDeleteAsync(ct);
    }
}