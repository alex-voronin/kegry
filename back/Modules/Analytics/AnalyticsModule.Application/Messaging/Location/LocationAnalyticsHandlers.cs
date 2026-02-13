using AnalyticsModule.Application.Services;
using LocationModule.Contracts.Events;
using PostHog;
using Wolverine.Attributes;

namespace AnalyticsModule.Application.Messaging.Location;

[WolverineHandler]
public static class LocationAnalyticsHandlers
{
    public static void Handle(TrackedUsersMetEvent @event, IAnalyticsService analyticsService)
    {
        if (@event.UserIds.Length != 2)
        {
            throw new ArgumentException("Only two users can be tracked");
        }

        var firstUserId = @event.UserIds[0];
        var secondUserId = @event.UserIds[1];
        analyticsService.Capture(
            distinctId: firstUserId.ToString(),
            eventName: AnalyticsEvents.Location.UsersMet,
            properties: new Dictionary<string, object>
            {
                { AnalyticsEvents.Keys.OtherUserId, secondUserId.ToString() }
            });
    }
}
