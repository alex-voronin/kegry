using FeedModule.Domain.Aggregates;
using SharedCore.Interfaces;

namespace FeedModule.Domain.Repositories;

public interface IFeedUserRelationRepository : IRepositoryOutbox<FeedUserRelation>
{
    public Task<FeedUserRelation?> GetAsync(
        Guid myUserId,
        Guid otherUserId,
        bool asNoTracking = false,
        CancellationToken ct = default);
    public Task<bool> IsInMeetingAsync(Guid userId, CancellationToken ct = default);
    public Task ResetMeetingsAsync(Guid[] userIds, CancellationToken ct = default);
    public Task ResetExpiredMeetingsAsync(CancellationToken ct = default);
}