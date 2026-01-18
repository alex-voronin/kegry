using SharedCore.Events;

namespace UserProfileModule.Contracts.Events;

public record UserProfileChangedEvent : UserEvent
{
    public required string Email { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public required string AvatarImageS3Key { get; init; }
}