using AnalyticsModule.Application.Services;
using EngagementModule.Contracts.Events;
using Wolverine.Attributes;

namespace AnalyticsModule.Application.Messaging.Engagement;

[WolverineHandler]
public static class EngagementAnalyticsHandlers
{
    public static void Handle(NpcInvitedEvent @event, IAnalyticsService analyticsService)
    {
        analyticsService.Capture(
            distinctId: @event.UserId.ToString(),
            eventName: AnalyticsEvents.Engagement.NpcInvited,
            properties: new Dictionary<string, object>
            {
                { AnalyticsEvents.Keys.NpcId, @event.NpcId.ToString() },
            });
    }
}
