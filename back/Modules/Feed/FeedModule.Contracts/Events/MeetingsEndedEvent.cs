namespace FeedModule.Contracts.Events;

public class MeetingsEndedEvent
{
    public required IReadOnlyList<Guid[]> ParticipantIdsFromDeletedChats { get; init; }
}