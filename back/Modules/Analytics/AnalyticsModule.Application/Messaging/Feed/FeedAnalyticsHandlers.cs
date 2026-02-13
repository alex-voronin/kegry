using AnalyticsModule.Application.Services;
using FeedModule.Contracts.Events;
using Wolverine.Attributes;

namespace AnalyticsModule.Application.Messaging.Feed;

[WolverineHandler]
public static class FeedAnalyticsHandlers
{
    public static void Handle(UserInvitedEvent @event, IAnalyticsService analyticsService)
    {
        analyticsService.Capture(
            distinctId: @event.ByUserId.ToString(),
            eventName: AnalyticsEvents.Feed.UserInvited,
            properties: new Dictionary<string, object>
            {
                { AnalyticsEvents.Keys.OtherUserId, @event.UserId.ToString() },
                { AnalyticsEvents.Keys.ExpiresAt, @event.MeetingExpiresAt.ToString("o") }
            });
    }
    
    public static void Handle(UserBlockedEvent @event, IAnalyticsService analyticsService)
    {
        analyticsService.Capture(
            distinctId: @event.ByUserId.ToString(),
            eventName: AnalyticsEvents.Feed.UserInvited,
            properties: new Dictionary<string, object>
            {
                { AnalyticsEvents.Keys.OtherUserId, @event.UserId.ToString() },
            });
    }

    public static void Handle(MeetingArrangedEvent @event, IAnalyticsService analyticsService)
    {
        if (@event.Participants.Length != 2)
        {
            throw new ArgumentException("Only two users can be tracked");
        }

        var firstUserId = @event.Participants[0].UserId;
        var secondUserId = @event.Participants[1].UserId;
        
        analyticsService.Capture(
            distinctId: firstUserId.ToString(),
            eventName: AnalyticsEvents.Feed.MeetingArranged,
            properties: new Dictionary<string, object>
            {
                { AnalyticsEvents.Keys.ExpiresAt, @event.ExpiresAt.ToString("o") },
                { AnalyticsEvents.Keys.OtherUserId, secondUserId.ToString() }
            });
    }

    public static void Handle(MeetingsEndedEvent @event, IAnalyticsService analyticsService)
    {
        foreach (var participantIds in @event.ParticipantIdsFromDeletedChats)
        {
            if (participantIds.Length != 2)
            {
                continue;
            }
            
            var firstUserId = participantIds[0];
            var secondUserId = participantIds[1];
            analyticsService.Capture(
                distinctId: firstUserId.ToString(),
                eventName: AnalyticsEvents.Feed.MeetingEnded,
                properties: new Dictionary<string, object>
                {
                    { AnalyticsEvents.Keys.OtherUserId, secondUserId.ToString() }
                });
        }
    }
}
