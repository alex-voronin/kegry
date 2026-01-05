using SharedCore.Events;

namespace UserProfileModule.Contracts.Events;

public class UserProfileChangedEvent : UserEvent
{
    public required string Email { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public required string AvatarImageUrl { get; init; }
}