namespace ChatModule.Contracts.Events;

public class ChatParticipantsMetEvent
{
    public required Guid[] ParticipantIds { get; init; }
}