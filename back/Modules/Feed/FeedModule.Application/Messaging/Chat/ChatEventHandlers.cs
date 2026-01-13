using ChatModule.Contracts.Events;
using FeedModule.Domain.Repositories;
using Wolverine.Attributes;

namespace FeedModule.Application.Messaging.Chat;

[WolverineHandler]
public class ChatEventHandlers
{
    public static async Task HandleAsync(
        ChatsDeletedEvent @event,
        IFeedUserRelationRepository userRelationRepository,
        CancellationToken ct = default)
    {
        var userIds = @event.ParticipantIdsFromDeletedChats.SelectMany(ids => ids).Distinct().ToArray();
        await userRelationRepository.SetShouldRateAsync(userIds, ct);
        await userRelationRepository.ResetMeetingsAsync(userIds, ct);
        await userRelationRepository.SaveChangesAsync(ct);
    }
}