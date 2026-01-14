using ChatModule.Contracts.Events;
using ChatModule.Domain.Repositories;
using LocationModule.Contracts.Events;
using Wolverine;
using Wolverine.Attributes;

namespace ChatModule.Application.Messaging.Location;

[WolverineHandler]
public static class ChatParticipantsMetEventHandler
{
    internal static TimeSpan MetEventDelay = TimeSpan.FromMinutes(30);

    public record DelayedWrapperEvent(TrackedUsersMetEvent @Event);

    public static ValueTask HandleAsync(
        TrackedUsersMetEvent @event,
        IMessageBus messageBus,
        CancellationToken ct)
    {
        // Delay to allow users to keep chatting each other for some time even after the scheduled meeting
        // It can't be scheduled anywhere else as scheduled events can have only one handler
        var wrapperEvent = new DelayedWrapperEvent(@event);
        return messageBus.ScheduleAsync(wrapperEvent, MetEventDelay);
    }

    public static async Task HandleAsync(
        DelayedWrapperEvent @event,
        IChatRepository chatRepository,
        CancellationToken ct)
    {
        var userId = @event.Event.UserIds.First();
        var chat = await chatRepository.GetActiveChatByUserId(userId, ct);
        if (chat != null)
        {
            chat.ParticipantsMet = true;
            await chatRepository.PublishEventAsync(
                new ChatParticipantsMetEvent() { ParticipantIds = @event.Event.UserIds }
                );
            await chatRepository.SaveChangesAndFlushMessagesAsync(ct);
        }
    }
}