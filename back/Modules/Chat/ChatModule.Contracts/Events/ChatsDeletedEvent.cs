namespace ChatModule.Contracts.Events;

public record ChatsDeletedEvent
{
    public required IReadOnlyList<Guid[]> ParticipantIdsFromDeletedChats { get; init; }
}