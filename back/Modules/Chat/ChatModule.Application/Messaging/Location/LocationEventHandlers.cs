using ChatModule.Domain.Repositories;
using ChatModule.Domain.Services;
using LocationModule.Contracts.Events;
using Wolverine;
using Wolverine.Attributes;

namespace ChatModule.Application.Messaging.Location;

[WolverineHandler]
public static class ChatParticipantsMetEventHandler
{
    private const int DeleteDelayHours = 1;

    public record PostponedDeleteChatCommand(Guid UserId);

    [Transactional]
    public static ValueTask HandleAsync(
        ChatParticipantsMetEvent @event,
        IMessageBus bus,
        CancellationToken ct)
    {
        // Delay to allow users to keep chatting each other for some time even after the scheduled meeting
        var userId = @event.ParticipantIds.First();
        return bus.ScheduleAsync(
            new PostponedDeleteChatCommand(userId), DateTimeOffset.UtcNow.AddHours(DeleteDelayHours));
    }

    public static Task HandleAsync(
        PostponedDeleteChatCommand command,
        IChatService chatService,
        CancellationToken ct)
    {
        return chatService.DeleteChatAsync(command.UserId, ct);
    }
}