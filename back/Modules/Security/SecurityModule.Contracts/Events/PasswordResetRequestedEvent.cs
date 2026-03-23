namespace SecurityModule.Contracts.Events;

public record PasswordResetRequestedEvent
{
    public required Guid UserId { get; init; }
    public required string Email { get; init; }
    public required string BaseUrl { get; init; }
    public required string ResetToken { get; init; }
}
