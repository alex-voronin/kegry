using Amazon.Util.Internal;
using FeedModule.Domain.Aggregates;
using FeedModule.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Persistence;
using SharedCore.Services.LifeTimes;
using Wolverine.EntityFrameworkCore;

namespace FeedModule.Infrastructure.Persistence.Repositories;

internal class EfCoreFeedUserRelationRepository : RepositoryOutbox<FeedContext, FeedUserRelation>,
    IFeedUserRelationRepository, IScoped
{
    private readonly TimeProvider _timeProvider;

    public EfCoreFeedUserRelationRepository(
        IDbContextOutbox<FeedContext> contextOutbox,
        TimeProvider timeProvider) : base(contextOutbox)
    {
        _timeProvider = timeProvider;
    }

    public async Task<FeedUserRelation?> GetAsync(Guid myUserId, Guid otherUserId, bool asNoTracking = false,
        CancellationToken ct = default)
    {
        var query = Context.FeedUserRelations.AsQueryable();
        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync(fp => fp.MyUserId == myUserId && fp.OtherUserId == otherUserId, ct);
    }

    public async Task<FeedUserRelation> GetOrCreateAsync(
        Guid myUserId,
        Guid otherUserId,
        bool asNoTracking = false,
        CancellationToken ct = default)
    {
        var feedUserRelation = await GetAsync(myUserId, otherUserId, asNoTracking, ct);
        if (feedUserRelation == null)
        {
            feedUserRelation = new FeedUserRelation
            {
                Id = Guid.CreateVersion7(),
                MyUserId = myUserId,
                OtherUserId = otherUserId,
                InvitedAt = DateTimeOffset.MinValue,
                InviteExpiresAt = DateTimeOffset.MinValue,
                SkippedAt = DateTimeOffset.MinValue,
                LastSeenAt = _timeProvider.GetUtcNow()
            };
            await AddAsync(feedUserRelation, ct);
        }

        return feedUserRelation;
    }

    public async Task<bool> IsInMeetingAsync(Guid userId, CancellationToken ct = default)
    {
        return await Context.FeedUserRelations.AnyAsync(fp => fp.MyUserId == userId && fp.IsInMeeting, ct);
    }

    public Task SetShouldRateAsync(Guid[] userIds, CancellationToken ct = default)
    {
        return Context.FeedUserRelations
            .Where(r => userIds.Contains(r.MyUserId) && r.IsInMeeting)
            .ExecuteUpdateAsync(setter =>
                    setter.SetProperty(r => r.ShouldRate, true),
                ct);
    }

    public Task ResetMeetingsAsync(Guid[] userIds, CancellationToken ct = default)
    {
        return Context.FeedUserRelations
            .Where(r => userIds.Contains(r.MyUserId) && r.IsInMeeting)
            .ExecuteUpdateAsync(setter =>
                    setter
                        .SetProperty(r => r.IsInMeeting, false)
                        // Set LastSeenAt just in case to move the other user down in the feed. 
                        .SetProperty(r => r.LastSeenAt, DateTimeOffset.UtcNow)
                        // Reset InviteExpiresAt to avoid a situation where you invite another user
                        // from the friends page after meeting ends and the next meeting is established immediately
                        // without accepting invite by another user
                        .SetProperty(r => r.InviteExpiresAt, DateTimeOffset.UtcNow),
                ct);
    }

    public Task ResetExpiredMeetingsAsync(CancellationToken ct = default)
    {
        return Context.FeedUserRelations
            .Where(r => r.IsInMeeting && r.InviteExpiresAt < DateTimeOffset.UtcNow)
            .ExecuteUpdateAsync(setter =>
                    setter.SetProperty(r => r.IsInMeeting, false),
                ct);
    }

    public Task<int> GetMadeInvitesCountAsync(Guid userId, DateOnly date, CancellationToken ct = default)
    {
        var start = new DateTimeOffset(date.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var end = start.AddDays(1);

        return Context.FeedUserRelations
            .Where(invite =>
                invite.MyUserId == userId &&
                invite.InvitedAt >= start && invite.InvitedAt < end &&
                !invite.IsBlocked)
            .Where(invite =>
                !Context.FeedUserRelations
                    .Any(meeting => meeting.MyUserId == invite.OtherUserId &&
                                    meeting.IsInMeeting))
            .Where(invite => !Context.Users
                .Any(user => user.Id == invite.OtherUserId && !user.IsPublic))
            .CountAsync(ct);
    }

    public async Task<(bool isInMeeting, Guid? userIdToRate)> GetFeedStatusAsync(Guid userId,
        CancellationToken ct = default)
    {
        var activeMeeting = await Context.FeedUserRelations
            .AsNoTracking()
            .Where(r => r.MyUserId == userId && r.IsInMeeting)
            .Select(r => new { r.OtherUserId })
            .FirstOrDefaultAsync(ct);

        if (activeMeeting != null)
        {
            return (true, null);
        }

        var userIdToRate = await Context.FeedUserRelations
            .AsNoTracking()
            .Where(r => r.MyUserId == userId && r.ShouldRate)
            .OrderByDescending(r => r.LastSeenAt)
            .Select(r => (Guid?)r.OtherUserId)
            .FirstOrDefaultAsync(ct);

        return (false, userIdToRate);
    }

    public Task DeleteForUserAsync(Guid userId, CancellationToken ct = default)
    {
        return Context.FeedUserRelations
            .Where(r => r.MyUserId == userId || r.OtherUserId == userId)
            .ExecuteDeleteAsync(ct);
    }
}