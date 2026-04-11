using FeedModule.Contracts.Events;
using FeedModule.Domain.Aggregates;
using FeedModule.Domain.Exceptions;
using FeedModule.Domain.Repositories;
using FeedModule.Domain.Resources;
using Microsoft.Extensions.Localization;
using Serilog;
using SharedCore.Interfaces;
using SharedCore.Services.LifeTimes;

namespace FeedModule.Domain.Services;

internal sealed class UserActionsService : IUserActionsService, IScoped
{
    private readonly IUserActionLimitsService _userActionLimitsService;
    private readonly IFeedUserRelationRepository _feedUserRelationRepository;
    private readonly IFilterRepository _filterRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUserRatingRepository _userRatingRepository;
    private readonly IUserContext _userContext;
    private readonly TimeProvider _timeProvider;
    private readonly IStringLocalizer<FeedLocalization> _localizer;

    public UserActionsService(
        IUserActionLimitsService userActionLimitsService,
        IFeedUserRelationRepository feedUserRelationRepository,
        IFilterRepository filterRepository,
        IUserRepository userRepository,
        IUserRatingRepository userRatingRepository,
        IUserContext userContext,
        TimeProvider timeProvider,
        IStringLocalizer<FeedLocalization> localizer)
    {
        _userActionLimitsService = userActionLimitsService;
        _feedUserRelationRepository = feedUserRelationRepository;
        _filterRepository = filterRepository;
        _userRepository = userRepository;
        _userRatingRepository = userRatingRepository;
        _userContext = userContext;
        _timeProvider = timeProvider;
        _localizer = localizer;
    }

    public async Task RateUserAsync(Guid myUserId, Guid otherUserId, decimal? rating, CancellationToken ct)
    {
        var transaction = await _userRatingRepository
            .And(_feedUserRelationRepository)
            .StartTransactionAsync(ct);

        var feedUserRelation = await _feedUserRelationRepository.GetOrCreateAsync(myUserId, otherUserId, ct: ct);
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

    public async Task BlockUserAsync(Guid myUserId, Guid otherUserId, string? reportMessage, CancellationToken ct)
    {
        var feedUserRelation = await _feedUserRelationRepository.GetOrCreateAsync(myUserId, otherUserId, ct: ct);
        feedUserRelation.IsBlocked = true;
        feedUserRelation.ReportMessage = reportMessage;
        feedUserRelation.LastSeenAt = _timeProvider.GetUtcNow();
        feedUserRelation.IsFriend = false;
        await _feedUserRelationRepository.PublishEventAsync(
            new UserBlockedEvent()
            {
                UserId = otherUserId,
                ByUserId = myUserId,
            });
        await _feedUserRelationRepository.SaveChangesAndFlushMessagesAsync(ct);
    }

    public async Task UnblockUserAsync(Guid myUserId, Guid otherUserId, CancellationToken ct)
    {
        var feedUserRelation = await _feedUserRelationRepository.GetOrCreateAsync(myUserId, otherUserId, ct: ct);
        feedUserRelation.IsBlocked = false;
        feedUserRelation.LastSeenAt = _timeProvider.GetUtcNow();
        await _feedUserRelationRepository.SaveChangesAsync(ct);
    }

    public async Task RemoveFriendAsync(Guid myUserId, Guid otherUserId, CancellationToken ct)
    {
        var feedUserRelation = await _feedUserRelationRepository.GetOrCreateAsync(myUserId, otherUserId, ct: ct);
        feedUserRelation.IsFriend = false;
        feedUserRelation.LastSeenAt = _timeProvider.GetUtcNow();
        await _feedUserRelationRepository.SaveChangesAsync(ct);
    }

    public async Task SkipUserAsync(Guid myUserId, Guid otherUserId, CancellationToken ct)
    {
        var feedUserRelation = await _feedUserRelationRepository.GetOrCreateAsync(myUserId, otherUserId, ct: ct);
        feedUserRelation.SkippedAt = _timeProvider.GetUtcNow();
        feedUserRelation.LastSeenAt = _timeProvider.GetUtcNow();
        await _feedUserRelationRepository.SaveChangesAsync(ct);
    }

    public async Task IgnoreUserAsync(Guid myUserId, Guid otherUserId, CancellationToken ct)
    {
        var feedUserRelation = await _feedUserRelationRepository.GetOrCreateAsync(myUserId, otherUserId, ct: ct);
        feedUserRelation.LastSeenAt = _timeProvider.GetUtcNow();
        await _feedUserRelationRepository.SaveChangesAsync(ct);
    }

    public Task InviteUserAsync(Guid myUserId, Guid otherUserId,
        DateTimeOffset expiresAt, CancellationToken ct)
    {
        return InviteUserAsync(
            myUserId: myUserId,
            otherUserId: otherUserId,
            userTimeZone: _userContext.UserTimeZone,
            expiresAt: expiresAt,
            ignoreAvailableInvites: false,
            throwInMeetingException: false,
            ct: ct
        );
    }

    public async Task InviteFriendAsync(Guid myUserId, Guid otherUserId, DateTimeOffset expiresAt, CancellationToken ct)
    {
        var meToOtherUserRelation = await _feedUserRelationRepository.GetAsync(myUserId, otherUserId,
            asNoTracking: false, ct: ct);
        if (meToOtherUserRelation == null || !meToOtherUserRelation.IsFriend)
        {
            throw FeedExceptions.UserIsNotFriend(otherUserId);
        }

        await InviteUserAsync(
            myUserId: myUserId,
            otherUserId: otherUserId,
            userTimeZone: _userContext.UserTimeZone,
            expiresAt: expiresAt,
            ignoreAvailableInvites: true,
            throwInMeetingException: false,
            ct: ct
        );
    }

    public async Task AcceptInviteAsync(Guid myUserId, Guid otherUserId, CancellationToken ct)
    {
        var now = _timeProvider.GetUtcNow();
        var otherToMeUserRelation = await _feedUserRelationRepository.GetAsync(otherUserId, myUserId, false, ct);
        if (otherToMeUserRelation == null || otherToMeUserRelation.IsInviteExpired(now))
        {
            throw FeedExceptions.InviteExpired(_localizer);
        }

        await InviteUserAsync(
            myUserId: myUserId,
            otherUserId: otherUserId,
            userTimeZone: _userContext.UserTimeZone,
            expiresAt: otherToMeUserRelation.InviteExpiresAt,
            ignoreAvailableInvites: true,
            throwInMeetingException: true,
            ct: ct
        );
    }

    public async Task<Filter> GetFilterAsync(Guid userId, CancellationToken ct)
    {
        var filter = await _filterRepository.GetByIdAsync(userId, asNoTracking: true, ct: ct);
        return filter ?? Filter.CreateDefault(userId);
    }

    public async Task UpdateFilterAsync(Filter filter, CancellationToken ct)
    {
        var existingFilter = await _filterRepository.GetByIdAsync(filter.UserId, ct: ct);
        if (existingFilter == null)
        {
            await _filterRepository.AddAsync(filter, ct);
        }
        else
        {
            existingFilter.RadiusMeters = filter.RadiusMeters;
            existingFilter.AgeFrom = filter.AgeFrom;
            existingFilter.AgeTo = filter.AgeTo;
            existingFilter.Gender = filter.Gender;
            existingFilter.HobbyIds = filter.HobbyIds;
        }

        await _filterRepository.SaveChangesAsync(ct);
    }

    // It's unsafe because can throw exception when two users invite the same user at the same time.
    // Since the user can be only in one meeting.
    private async Task InviteUserUnsafeAsync(Guid myUserId, Guid otherUserId, DateTimeOffset expiresAt,
        bool throwInMeetingException, CancellationToken ct = default)
    {
        var now = _timeProvider.GetUtcNow();
        var meToOtherUserRelation = await _feedUserRelationRepository.GetOrCreateAsync(myUserId, otherUserId, ct: ct);

        if (meToOtherUserRelation.InviteExpiresAt > now)
        {
            // Do nothing if already invited
            return;
        }

        meToOtherUserRelation.InvitedAt = now;
        meToOtherUserRelation.InviteExpiresAt = expiresAt;
        meToOtherUserRelation.LastSeenAt = now;

        var otherToMeUserRelation = await _feedUserRelationRepository.GetAsync(otherUserId, myUserId, false, ct);
        if (otherToMeUserRelation != null && otherToMeUserRelation.IsBlocked)
        {
            // Save changes to consume the invite and return silently to imitate a successful user invitation,
            // regardless of the blocked state.
            await _feedUserRelationRepository.SaveChangesAsync(ct);
            return;
        }

        if (otherToMeUserRelation != null && !otherToMeUserRelation.IsInviteExpired(now))
        {
            var succeed = await TryArrangeMeeting(
                meToOtherUserRelation: meToOtherUserRelation,
                otherToMeUserRelation: otherToMeUserRelation,
                ct: ct);
            if (!succeed && throwInMeetingException)
            {
                throw FeedExceptions.UserAlreadyInMeeting(_localizer);
            }
        }
        else
        {
            await _feedUserRelationRepository.PublishEventAsync(
                new UserInvitedEvent()
                {
                    UserId = otherUserId,
                    ByUserId = myUserId,
                    InviteExpiresAt = meToOtherUserRelation.InviteExpiresAt
                });
        }

        await _feedUserRelationRepository.SaveChangesAndFlushMessagesAsync(ct);
    }

    private async Task InviteUserAsync(
        Guid myUserId,
        Guid otherUserId,
        TimeZoneInfo userTimeZone,
        DateTimeOffset expiresAt,
        bool ignoreAvailableInvites,
        bool throwInMeetingException,
        CancellationToken ct = default)
    {
        var now = _timeProvider.GetUtcNow();
        if (expiresAt > now.AddHours(48))
        {
            throw FeedExceptions.InviteExpirationTooFar();
        }

        if (!ignoreAvailableInvites)
        {
            var availableInvitesCount = await _userActionLimitsService.GetAvailableInvitesAsync(
                userId: myUserId,
                userTimeZone: userTimeZone,
                ct: ct);
            if (availableInvitesCount <= 0)
            {
                throw FeedExceptions.NoAvailableInvitesLeft(myUserId);
            }
        }

        try
        {
            await InviteUserUnsafeAsync(
                myUserId: myUserId,
                otherUserId: otherUserId,
                expiresAt: expiresAt,
                throwInMeetingException: throwInMeetingException,
                ct: ct);
        }
        catch (Exception e)
        {
            var isInMeeting = await _feedUserRelationRepository.IsInMeetingAsync(otherUserId, ct);
            if (!isInMeeting || throwInMeetingException)
            {
                throw;
            }

            Log.Information(e, "User {UserId} tried to invite {OtherUserId}, but he is in meeting already",
                myUserId, otherUserId);
        }
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

        var meetingExpiresAt = meToOtherUserRelation.InviteExpiresAt < otherToMeUserRelation.InviteExpiresAt
            ? meToOtherUserRelation.InviteExpiresAt
            : otherToMeUserRelation.InviteExpiresAt;

        otherToMeUserRelation.IsFriend = true;
        otherToMeUserRelation.IsInMeeting = true;
        otherToMeUserRelation.InviteExpiresAt = meetingExpiresAt;
        meToOtherUserRelation.IsFriend = true;
        meToOtherUserRelation.IsInMeeting = true;
        meToOtherUserRelation.InviteExpiresAt = meetingExpiresAt;
        // the potential race condition when multiple users invite the same user at the same time
        // is fixed by unq_feed_user_relation_active_meeting_per_user index

        var me = await _userRepository.GetByIdAsync(meToOtherUserRelation.MyUserId, asNoTracking: true, ct: ct);
        var other = await _userRepository.GetByIdAsync(meToOtherUserRelation.OtherUserId, asNoTracking: true,
            ct: ct);
        await _feedUserRelationRepository.PublishEventAsync(
            new MeetingArrangedEvent
            {
                ExpiresAt = meetingExpiresAt,
                Participants =
                [
                    new MeetingArrangedEvent.Participant(meToOtherUserRelation.MyUserId, me!.FullName),
                    new MeetingArrangedEvent.Participant(meToOtherUserRelation.OtherUserId, other!.FullName)
                ]
            });
        return true;
    }
}