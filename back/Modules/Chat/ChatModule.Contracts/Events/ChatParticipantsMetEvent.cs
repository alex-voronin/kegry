namespace ChatModule.Contracts.Events;

public record ChatParticipantsMetEvent
{
    public required Guid[] ParticipantIds { get; init; }
}