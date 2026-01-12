namespace ChatModule.Contracts.Events;

public class ChatsDeletedEvent
{
    public required IReadOnlyList<Guid[]> ParticipantIdsFromDeletedChats { get; init; }
}