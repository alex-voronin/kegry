namespace ChatModule.Contracts.Events;

public class ChatsDeletedEvent
{
    public required Guid[] UserIdsInDeletedChats { get; init; }
}