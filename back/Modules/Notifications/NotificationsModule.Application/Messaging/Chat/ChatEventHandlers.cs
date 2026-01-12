using ChatModule.Contracts.Events;
using NotificationsModule.Application.Services;
using Wolverine.Attributes;

namespace NotificationsModule.Application.Messaging.Chat;

[WolverineHandler]
public class ChatEventHandlers
{
    public static async Task HandleAsync(
        ChatCreatedEvent @event,
        IDebouncedEventHandler<MeetingArrangedNotificationEvent> eventHandler,
        CancellationToken ct = default)
    {
        foreach (var participant in @event.Participants)
        {
            await eventHandler.HandleAsync(
                new MeetingArrangedNotificationEvent
                {
                    UserId = participant.UserId,
                    OtherUserFullName = participant.FullName
                }, ct);
        }
    }
}