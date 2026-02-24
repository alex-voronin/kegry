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
    public EfCoreFeedUserRelationRepository(IDbContextOutbox<FeedContext> contextOutbox) : base(contextOutbox)
    {
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
                    setter.SetProperty(r => r.IsInMeeting, false),
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
}