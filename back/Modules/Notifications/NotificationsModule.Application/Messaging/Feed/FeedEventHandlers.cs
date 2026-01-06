using FeedModule.Contracts.Events;
using NotificationsModule.Application.Services;
using Wolverine.Attributes;

namespace NotificationsModule.Application.Messaging.Feed;

[WolverineHandler]
public class FeedEventHandlers
{
    public static async Task HandleAsync(
        MeetingArrangedEvent @event,
        INotificationManager notificationManager,
        CancellationToken ct = default)
    {
        // there is no need to debounce this event, as it can’t happen frequently.
        await notificationManager.SendMeetingArranged(@event.FirstUserId, @event.SecondUserFullName, ct);
        await notificationManager.SendMeetingArranged(@event.SecondUserId, @event.FirstUserFullName, ct);
    }
}