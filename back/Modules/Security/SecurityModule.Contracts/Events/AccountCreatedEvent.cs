using SharedCore.Events;

namespace SecurityModule.Contracts.Events;

public record AccountCreatedEvent : UserEvent
{
    public required string Email { get; init; }
    public required string? ConfirmationCode { get; init; }
    public required string Locale { get; init; }
}