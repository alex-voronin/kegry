using ChatModule.Contracts.Events;
using Wolverine.Attributes;

namespace NotificationsModule.Application.Messaging.Chat;

[WolverineHandler]
public class ChatEventHandlers
{
    public static async Task HandleAsync(
        ChatCreatedEvent @event,
        MeetingArrangedEventHandler eventHandler,
        CancellationToken ct = default)
    {
        await eventHandler.HandleAsync(
            new MeetingArrangedNotificationEvent
            {
                UserId = @event.FirstUserId,
                OtherUserFullName = @event.SecondUserFullName
            }, ct);
        
        await eventHandler.HandleAsync(
            new MeetingArrangedNotificationEvent
            {
                UserId = @event.SecondUserId,
                OtherUserFullName = @event.FirstUserFullName
            }, ct);
    }
}