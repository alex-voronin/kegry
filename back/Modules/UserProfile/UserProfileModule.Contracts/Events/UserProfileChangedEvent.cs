using SharedCore.Events;

namespace UserProfileModule.Contracts.Events;

public record UserProfileChangedEvent : UserEvent
{
    public required bool IsNewProfile { get; init; }
    public Guid? ReferrerId { get; init; }
    public required string Email { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public required string AvatarImageS3Key { get; init; }
    public required string? Locale { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
}