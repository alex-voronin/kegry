using ChatModule.Contracts.Events;
using FeedModule.Domain.Repositories;
using Wolverine.Attributes;

namespace FeedModule.Application.Messaging.Chat;

[WolverineHandler]
public class ChatEventHandlers
{
    public static Task HandleAsync(
        ChatsDeletedEvent @event,
        IFeedUserRelationRepository userRelationRepository,
        CancellationToken ct = default)
    {
        return userRelationRepository.ResetInMeetingAsync(@event.UserIdsInDeletedChats, ct);
    }
}