namespace ChatModule.Contracts.Events;

public class ChatCreatedEvent
{
    public record Participant(Guid UserId, string FullName);

    public Participant[] Participants { get; init; } = [];
}