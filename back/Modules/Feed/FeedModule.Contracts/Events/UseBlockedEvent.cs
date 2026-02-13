using SharedCore.Events;

namespace FeedModule.Contracts.Events;

public record UserBlockedEvent : UserEvent
{
    public required Guid ByUserId { get; init; }
}