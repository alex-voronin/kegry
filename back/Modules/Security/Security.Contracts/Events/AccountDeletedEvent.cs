namespace Security.Contracts.Events;

public class AccountDeletedEvent
{
    public required Guid Id { get; init; }
}