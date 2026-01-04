using SharedCore.Events;

namespace SecurityModule.Contracts.Events;

public class AccountCreatedEvent : UserEvent
{
    public required string Email { get; init; }
    public required string ConfirmationCode { get; init; }
}