namespace ChatModule.Contracts.Events;

public class ChatCreatedEvent
{
    public record Participant(Guid UserId, string FullName);

    public required DateTimeOffset ExpiresAt { get; init; }
    public required Participant[] Participants { get; init; } = [];
}