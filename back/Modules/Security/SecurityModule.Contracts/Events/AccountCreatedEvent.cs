namespace SecurityModule.Contracts.Events;

public class AccountCreatedEvent
{
    public required Guid Id { get; init; }
    public required string Email { get; init; }
    public required string ConfirmationCode { get; init; }
}