namespace SecurityModule.Contracts.Events;

public class PasswordResetRequestedEvent
{
    public required Guid UserId { get; init; }
    public required string Email { get; init; }
    public required string ResetToken { get; init; }
}
