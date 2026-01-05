using SharedCore.Events;

namespace FeedModule.Contracts.Events;

public class UserInvitedEvent : UserEvent
{
    public required Guid ByUserId { get; init; }
    public required DateTimeOffset MeetingExpiresAt { get; init; }
}