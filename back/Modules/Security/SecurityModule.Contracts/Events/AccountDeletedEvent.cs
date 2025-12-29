namespace SecurityModule.Contracts.Events;

public class AccountDeletedEvent
{
    public required Guid Id { get; init; }
}