using FeedModule.Contracts.Events;
using FeedModule.Domain.Aggregates;
using FeedModule.Domain.Repositories;
using SharedCore.Services.LifeTimes;

namespace FeedModule.Domain.Services;

internal sealed class UserActionsService : IUserActionsService, IScoped
{
    private readonly IFeedUserRelationRepository _feedUserRelationRepository;
    private readonly IFilterRepository _filterRepository;
    private readonly IUserProfileRepository _userProfileRepository;
    private readonly IUserRatingRepository _userRatingRepository;
    private readonly TimeProvider _timeProvider;

    public UserActionsService(
        IFeedUserRelationRepository feedUserRelationRepository,
        IFilterRepository filterRepository,
        IUserProfileRepository userProfileRepository,
        IUserRatingRepository userRatingRepository,
        TimeProvider timeProvider)
    {
        _feedUserRelationRepository = feedUserRelationRepository;
        _filterRepository = filterRepository;
        _userProfileRepository = userProfileRepository;
        _userRatingRepository = userRatingRepository;
        _timeProvider = timeProvider;
    }

    public async Task RateUserAsync(Guid myUserId, Guid otherUserId, decimal? rating, CancellationToken ct)
    {
        var transaction = await _userRatingRepository
            .And(_feedUserRelationRepository)
            .StartTransactionAsync(ct);

        var feedUserRelation = await GetOrCreateUserRelation(myUserId, otherUserId, ct);
        feedUserRelation.ShouldRate = false;
        
        if (rating != null)
        {
            var oldRating = feedUserRelation.Rating ?? 0.0m;
            await _userRatingRepository.UpsertRatingAtomic(
                userId: otherUserId,
                oldRating: oldRating,
                newRating: rating.Value,
                ct: ct);

            feedUserRelation.Rating = rating;
            feedUserRelation.LastSeenAt = _timeProvider.GetUtcNow();
        }

        await transaction.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }

    public async Task BlockUserAsync(Guid myUserId, Guid otherUserId, CancellationToken ct)
    {
        var feedUserRelation = await GetOrCreateUserRelation(myUserId, otherUserId, ct);
        feedUserRelation.IsBlocked = true;
        feedUserRelation.LastSeenAt = _timeProvider.GetUtcNow();
        await _feedUserRelationRepository.SaveChangesAsync(ct);
    }

    public async Task SkipUserAsync(Guid myUserId, Guid otherUserId, CancellationToken ct)
    {
        var feedUserRelation = await GetOrCreateUserRelation(myUserId, otherUserId, ct);
        feedUserRelation.SkippedAt = _timeProvider.GetUtcNow();
        feedUserRelation.LastSeenAt = _timeProvider.GetUtcNow();
        await _feedUserRelationRepository.SaveChangesAsync(ct);
    }

    public async Task InviteUserAsync(Guid myUserId, Guid otherUserId, CancellationToken ct)
    {
        var filter = await _filterRepository.GetByIdAsync(myUserId, asNoTracking: true, ct: ct);
        filter ??= Filter.CreateDefault(myUserId);

        var now = _timeProvider.GetUtcNow();
        var meToOtherUserRelation = await GetOrCreateUserRelation(myUserId, otherUserId, ct);
        meToOtherUserRelation.MeetingExpiresAt =
            _timeProvider.GetUtcNow().AddMinutes(filter.GetMeetingExpirationMinutesFromNow());
        meToOtherUserRelation.LastSeenAt = now;

        var otherToMeUserRelation = await _feedUserRelationRepository.GetAsync(otherUserId, myUserId, false, ct);
        if (otherToMeUserRelation != null && !otherToMeUserRelation.IsMeetingExpired(now))
        {
            await TryArrangeMeeting(otherToMeUserRelation, meToOtherUserRelation, ct);
        }
        else
        {
            await _feedUserRelationRepository.PublishEventAsync(
                new UserInvitedEvent()
                {
                    UserId = otherUserId,
                    ByUserId = myUserId,
                    MeetingExpiresAt = meToOtherUserRelation.MeetingExpiresAt
                });
        }

        await _feedUserRelationRepository.SaveChangesAndFlushMessagesAsync(ct);
    }

    private async Task<FeedUserRelation> GetOrCreateUserRelation(Guid myUserId, Guid otherUserId, CancellationToken ct)
    {
        var feedUserRelation = await _feedUserRelationRepository.GetAsync(myUserId, otherUserId, ct: ct);
        if (feedUserRelation == null)
        {
            feedUserRelation = new FeedUserRelation
            {
                Id = Guid.CreateVersion7(),
                MyUserId = myUserId,
                OtherUserId = otherUserId,
                MeetingExpiresAt = DateTimeOffset.MinValue,
                SkippedAt = DateTimeOffset.MinValue,
                LastSeenAt = _timeProvider.GetUtcNow()
            };
            await _feedUserRelationRepository.AddAsync(feedUserRelation, ct);
        }

        return feedUserRelation;
    }

    private async Task<bool> TryArrangeMeeting(
        FeedUserRelation meToOtherUserRelation,
        FeedUserRelation otherToMeUserRelation,
        CancellationToken ct)
    {
        var isInMeeting = await _feedUserRelationRepository.IsInMeetingAsync(meToOtherUserRelation.OtherUserId, ct);
        if (isInMeeting)
        {
            return false;
        }

        otherToMeUserRelation.MeetingExpiresAt = meToOtherUserRelation.MeetingExpiresAt;
        otherToMeUserRelation.IsInMeeting = true;
        meToOtherUserRelation.IsInMeeting = true;
        // the potential race condition when multiple users invite the same user at the same time
        // is fixed by unq_feed_user_relation_active_meeting_per_user index

        var me = await _userProfileRepository.GetByIdAsync(meToOtherUserRelation.MyUserId, asNoTracking: true, ct: ct);
        var other = await _userProfileRepository.GetByIdAsync(meToOtherUserRelation.OtherUserId, asNoTracking: true,
            ct: ct);
        await _feedUserRelationRepository.PublishEventAsync(
            new MeetingArrangedEvent
            {
                ExpiresAt = meToOtherUserRelation.MeetingExpiresAt,
                Participants =
                [
                    new MeetingArrangedEvent.Participant(meToOtherUserRelation.MyUserId, me!.FullName),
                    new MeetingArrangedEvent.Participant(meToOtherUserRelation.OtherUserId, other!.FullName)
                ]
            });
        return true;
    }
}