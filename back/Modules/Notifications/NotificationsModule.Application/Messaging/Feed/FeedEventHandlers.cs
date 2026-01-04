using FeedModule.Contracts.Events;
using NotificationsModule.Application.Services;
using Wolverine.Attributes;

namespace NotificationsModule.Application.Messaging.Feed;

[WolverineHandler]
public class FeedEventHandlers
{
    public static async Task HandleAsync(
        MeetingArrangedEvent @event,
        IPushService pushService,
        CancellationToken ct = default)
    {
        // there is no need to debounce this event, as it can’t happen frequently.
        await pushService.SendMeetingArrangedPush(@event.FirstUserId, @event.SecondUserFullName, ct);
        await pushService.SendMeetingArrangedPush(@event.SecondUserId, @event.FirstUserFullName, ct);
    }
}