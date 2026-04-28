using SharedCore.Events;

namespace SecurityModule.Contracts.Events;

public record PasswordResetRequestedEvent : UserEvent
{
    public required string Email { get; init; }
    public required string BaseUrl { get; init; }
    public required string ResetToken { get; init; }
}
