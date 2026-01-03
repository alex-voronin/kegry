namespace FeedModule.Contracts.Events;

public class UserInvitedEvent
{
    public required Guid UserId { get; init; }
    public required Guid ByUserId { get; init; }
}