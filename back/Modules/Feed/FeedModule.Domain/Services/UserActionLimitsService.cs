using FeedModule.Domain.Repositories;
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

    public async Task<int> GetAvailableInvitesAsync(Guid userId, CancellationToken ct = default)
    {
        var now = _timeProvider.GetUtcNow().Date;
        var madeInvites = await _feedUserRelationRepository.GetMadeInvitesCountAsync(userId,
            DateOnly.FromDateTime(now), ct);
        return Math.Max(FeedParameters.DefaultAvailableInvitesPerDay - madeInvites, 0);
    }
}