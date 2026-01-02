using FeedModule.Domain.Aggregates;
using SharedCore.Interfaces;

namespace FeedModule.Domain.Repositories;

public interface IFeedProfileRepository : IRepositoryOutbox<FeedProfile>
{
    public Task<FeedProfile?> GetAsync(
        Guid myUserId,
        Guid otherUserId,
        bool asNoTracking = false,
        CancellationToken ct = default);
    public Task<bool> IsInMeetingAsync(Guid userId, CancellationToken ct = default);
}