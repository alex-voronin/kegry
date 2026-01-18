namespace FeedModule.Contracts.Events;

public record MeetingArrangedEvent
{
    public record Participant(Guid UserId, string FullName);

    public DateTimeOffset ExpiresAt { get; init; }
    public Participant[] Participants { get; init; } = [];
}