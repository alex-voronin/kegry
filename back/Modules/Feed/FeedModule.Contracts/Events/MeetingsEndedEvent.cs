namespace FeedModule.Contracts.Events;

public record MeetingsEndedEvent
{
    public required IReadOnlyList<Guid[]> ParticipantIdsFromDeletedChats { get; init; }
}