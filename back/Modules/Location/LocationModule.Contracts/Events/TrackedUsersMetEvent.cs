namespace LocationModule.Contracts.Events;

public record TrackedUsersMetEvent
{
    public required Guid[] UserIds { get; init; }
}
