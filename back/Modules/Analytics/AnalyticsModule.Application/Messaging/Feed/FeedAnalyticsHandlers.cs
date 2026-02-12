using FeedModule.Contracts.Events;
using PostHog;
using Wolverine.Attributes;

namespace AnalyticsModule.Application.Messaging.Feed;

[WolverineHandler]
public class FeedAnalyticsHandlers(IPostHogClient postHogClient)
{
    public void Handle(UserInvitedEvent @event)
    {
        postHogClient.Capture(
            distinctId: @event.ByUserId.ToString(),
            eventName: AnalyticsEvents.Feed.UserInvited,
            properties: new Dictionary<string, object>
            {
                { "other_user_id", @event.UserId.ToString() },
                { "expires_at", @event.MeetingExpiresAt.ToString("o") }
            });
    }

    public void Handle(MeetingArrangedEvent @event)
    {
        if (@event.Participants.Length != 2)
        {
            throw new ArgumentException("Only two users can be tracked");
        }

        var firstUserId = @event.Participants[0].UserId;
        var secondUserId = @event.Participants[1].UserId;
        
        postHogClient.Capture(
            distinctId: firstUserId.ToString(),
            eventName: AnalyticsEvents.Feed.MeetingArranged,
            properties: new Dictionary<string, object>
            {
                { "expires_at", @event.ExpiresAt.ToString("o") },
                { "other_user_id", secondUserId.ToString() }
            });
    }

    public void Handle(MeetingsEndedEvent @event)
    {
        foreach (var participantIds in @event.ParticipantIdsFromDeletedChats)
        {
            if (participantIds.Length != 2)
            {
                continue;
            }
            
            var firstUserId = participantIds[0];
            var secondUserId = participantIds[1];
            postHogClient.Capture(
                distinctId: firstUserId.ToString(),
                eventName: AnalyticsEvents.Feed.MeetingEnded,
                properties: new Dictionary<string, object>
                {
                    { "other_user_id", secondUserId.ToString() }
                });
        }
    }
}
