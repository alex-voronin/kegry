namespace SecurityModule.Contracts.Events;

public record AccountDeletedEvent
{
    public required Guid Id { get; init; }
}