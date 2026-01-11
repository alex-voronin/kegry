namespace FeedModule.Contracts.Events;

public class MeetingArrangedEvent
{
    public DateTimeOffset MeetingExpiresAt { get; set; }
    public required Guid FirstUserId { get; init; }
    public required string FirstUserFullName { get; init; }
    public required Guid SecondUserId { get; init; }
    public required string SecondUserFullName { get; init; }
}