using LocationModule.Domain.Aggregates;
using SharedCore.Interfaces;

namespace LocationModule.Domain.Repositories;

public interface ITrackedUserPairRepository : IRepositoryOutbox<TrackedUserPair>
{
    Task<TrackedUserPair[]> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task DeleteByUserIds(Guid[] userIds, CancellationToken ct = default);
}