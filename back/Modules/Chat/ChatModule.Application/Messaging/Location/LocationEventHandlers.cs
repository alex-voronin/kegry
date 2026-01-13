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
    public static async Task HandleAsync(
        ChatParticipantsMetEvent @event,
        IChatRepository chatRepository,
        CancellationToken ct)
    {
        var userId = @event.ParticipantIds.First();
        var chat = await chatRepository.GetActiveChatByUserId(userId, ct);
        if (chat != null)
        {
            chat.ParticipantsMet = true;
            await chatRepository.SaveChangesAsync(ct);
        }
    }
}