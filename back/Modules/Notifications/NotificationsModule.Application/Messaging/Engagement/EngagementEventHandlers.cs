using EngagementModule.Contracts.Events;
using NotificationsModule.Application.Services;
using Serilog;
using Wolverine.Attributes;

namespace NotificationsModule.Application.Messaging.Engagement;

[WolverineHandler]
public static class EngagementEventHandlers
{
    public static async Task HandleAsync(
        ReferrerRewardReceivedEvent @event,
        IPushNotificationStrategy pushNotificationStrategy,
        CancellationToken ct = default)
    {
        Log.Information("{HandlerClass} Handling event {@Event}", typeof(EngagementEventHandlers).FullName, @event);
        // There is no need to debounce these events as we send them only when a really new 
        // NPC is added, and we don't have many of them
        await pushNotificationStrategy.SendReferrerRewardReceived(@event.UserId, ct: ct);
    }
}