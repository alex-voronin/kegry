using LocationModule.Contracts.Events;
using PostHog;
using Wolverine.Attributes;

namespace AnalyticsModule.Application.Messaging.Location;

[WolverineHandler]
public class LocationAnalyticsHandlers(IPostHogClient postHogClient)
{
    public void Handle(TrackedUsersMetEvent @event)
    {
        if (@event.UserIds.Length != 2)
        {
            throw new ArgumentException("Only two users can be tracked");
        }

        var firstUserId = @event.UserIds[0];
        var secondUserId = @event.UserIds[1];
        postHogClient.Capture(
            distinctId: firstUserId.ToString(),
            eventName: AnalyticsEvents.Location.UsersMet,
            properties: new Dictionary<string, object>
            {
                { "other_user_id", secondUserId.ToString() }
            });
    }
}
