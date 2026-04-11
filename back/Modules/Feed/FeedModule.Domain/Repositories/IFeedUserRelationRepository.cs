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

    public Task<FeedUserRelation> GetOrCreateAsync(
        Guid myUserId,
        Guid otherUserId,
        bool asNoTracking = false,
        CancellationToken ct = default);

    public Task<bool> IsInMeetingAsync(Guid userId, CancellationToken ct = default);
    public Task SetShouldRateAsync(Guid[] userIds, CancellationToken ct = default);
    public Task ResetMeetingsAsync(Guid[] userIds, CancellationToken ct = default);
    public Task ResetExpiredMeetingsAsync(CancellationToken ct = default);

    public Task<int> GetMadeInvitesCountAsync(Guid userId,
        DateTimeOffset userDateStartUtc,
        DateTimeOffset userDateEndUtc,
        CancellationToken ct = default);

    public Task DeleteForUserAsync(Guid userId, CancellationToken ct = default);

    public Task<(bool isInMeeting, Guid? userIdToRate)> GetFeedStatusAsync(
        Guid userId,
        CancellationToken ct = default);
}