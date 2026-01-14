namespace LocationModule.Contracts.Events;

public class TrackedUsersMetEvent
{
    public required Guid[] UserIds { get; init; }
}
