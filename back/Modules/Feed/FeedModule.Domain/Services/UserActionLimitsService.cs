using FeedModule.Domain.Repositories;
using SharedCore.Extensions;
using SharedCore.Interfaces;
using SharedCore.Services.LifeTimes;

namespace FeedModule.Domain.Services;

internal class UserActionLimitsService : IUserActionLimitsService, IScoped
{
    private readonly IFeedUserRelationRepository _feedUserRelationRepository;
    private readonly TimeProvider _timeProvider;

    public UserActionLimitsService(IFeedUserRelationRepository feedUserRelationRepository, TimeProvider timeProvider)
    {
        _feedUserRelationRepository = feedUserRelationRepository;
        _timeProvider = timeProvider;
    }

    public async Task<int> GetAvailableInvitesAsync(Guid userId, TimeZoneInfo userTimeZone,
        CancellationToken ct = default)
    {
        var now = _timeProvider.GetUtcNow();
        var (userDateStartUtc, userDateEndUtc) = now.GetDayBoundsInUtc(userTimeZone);

        var madeInvites = await _feedUserRelationRepository.GetMadeInvitesCountAsync(
            userId: userId,
            userDateStartUtc: userDateStartUtc,
            userDateEndUtc: userDateEndUtc,
            ct: ct);
        return Math.Max(FeedParameters.DefaultAvailableInvitesPerDay - madeInvites, 0);
    }
}