namespace FeedModule.Contracts.Events;

public class MeetingArrangedEvent
{
    public record Participant(Guid UserId, string FullName);

    public DateTimeOffset MeetingExpiresAt { get; init; }
    public Participant[] Participants { get; init; } = [];
}