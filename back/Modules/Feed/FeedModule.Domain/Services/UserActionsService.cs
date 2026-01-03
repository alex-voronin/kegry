using FeedModule.Application.Services;
using FeedModule.Contracts.Events;
using FeedModule.Domain.Aggregates;
using FeedModule.Domain.Repositories;
using SharedCore.Services.LifeTimes;

namespace FeedModule.Domain.Services;

internal sealed class UserActionsService : IUserActionsService, IScoped
{
    private readonly IFeedProfileRepository _feedProfileRepository;
    private readonly IFilterRepository _filterRepository;
    private readonly IUserProfileRepository _userProfileRepository;
    private readonly TimeProvider _timeProvider;

    public UserActionsService(
        IFeedProfileRepository feedProfileRepository,
        IFilterRepository filterRepository,
        IUserProfileRepository userProfileRepository,
        TimeProvider timeProvider)
    {
        _feedProfileRepository = feedProfileRepository;
        _filterRepository = filterRepository;
        _userProfileRepository = userProfileRepository;
        _timeProvider = timeProvider;
    }

    public async Task RateUserAsync(Guid myUserId, Guid otherUserId, decimal rating, CancellationToken ct)
    {
        var profile = await GetOrCreateProfile(myUserId, otherUserId, ct);
        profile.Rating = rating;
        profile.LastSeenAt = _timeProvider.GetUtcNow();
        _feedProfileRepository.Update(profile);
        await _feedProfileRepository.SaveChangesAsync(ct);
    }

    public async Task BlockUserAsync(Guid myUserId, Guid otherUserId, CancellationToken ct)
    {
        var profile = await GetOrCreateProfile(myUserId, otherUserId, ct);
        profile.IsBlocked = true;
        profile.LastSeenAt = _timeProvider.GetUtcNow();
        _feedProfileRepository.Update(profile);
        await _feedProfileRepository.SaveChangesAsync(ct);
    }

    public async Task SkipUserAsync(Guid myUserId, Guid otherUserId, CancellationToken ct)
    {
        var profile = await GetOrCreateProfile(myUserId, otherUserId, ct);
        profile.SkippedAt = _timeProvider.GetUtcNow();
        profile.LastSeenAt = _timeProvider.GetUtcNow();
        _feedProfileRepository.Update(profile);
        await _feedProfileRepository.SaveChangesAsync(ct);
    }

    public async Task InviteUserAsync(Guid myUserId, Guid otherUserId, CancellationToken ct)
    {
        var filter = await _filterRepository.GetByIdAsync(myUserId, asNoTracking: true, ct: ct);
        filter ??= Filter.CreateDefault(myUserId);

        var myProfile = await GetOrCreateProfile(myUserId, otherUserId, ct);
        myProfile.MeetingTimeExpiresAt =
            _timeProvider.GetUtcNow().AddMinutes(filter.GetMeetingExpirationMinutesFromNow());
        myProfile.LastSeenAt = _timeProvider.GetUtcNow();
        _feedProfileRepository.Update(myProfile);

        var otherProfile = await _feedProfileRepository.GetAsync(otherUserId, myUserId, false, ct);
        if (otherProfile != null && otherProfile.MeetingTimeExpiresAt < myProfile.LastSeenAt)
        {
            var isInMeeting = await _feedProfileRepository.IsInMeetingAsync(otherUserId, ct);
            if (!isInMeeting)
            {
                otherProfile.MeetingTimeExpiresAt = myProfile.MeetingTimeExpiresAt;
                otherProfile.IsInMeeting = true;
                myProfile.IsInMeeting = true;
                // the potential race condition when multiple users invite the same user at the same time
                // is fixed by unq_feed_profile_active_meeting_per_user index
            }

            var me = await _userProfileRepository.GetByIdAsync(myUserId, asNoTracking: true, ct:ct);
            var other = await _userProfileRepository.GetByIdAsync(otherUserId, asNoTracking: true, ct:ct);
            await _feedProfileRepository.PublishEventAsync(
                new UsersMutuallyInvited
                {
                    FirstUserId = myUserId,
                    FirstUserFullName = me!.FullName,
                    SecondUserId = otherUserId,
                    SecondUserFullName = other!.FullName
                }, ct: ct);
        }

        await _feedProfileRepository.SaveChangesAndFlushMessagesAsync(ct);
    }

    private async Task<FeedProfile> GetOrCreateProfile(Guid myUserId, Guid otherUserId, CancellationToken ct)
    {
        var profile = await _feedProfileRepository.GetAsync(myUserId, otherUserId, false, ct);
        if (profile == null)
        {
            profile = new FeedProfile
            {
                Id = Guid.CreateVersion7(),
                MyUserId = myUserId,
                OtherUserId = otherUserId,
                Rating = 0,
                IsBlocked = false,
                IsInMeeting = false,
                MeetingTimeExpiresAt = DateTimeOffset.MinValue,
                SkippedAt = DateTimeOffset.MinValue,
                LastSeenAt = _timeProvider.GetUtcNow()
            };
            await _feedProfileRepository.AddAsync(profile, ct);
        }

        return profile;
    }
}