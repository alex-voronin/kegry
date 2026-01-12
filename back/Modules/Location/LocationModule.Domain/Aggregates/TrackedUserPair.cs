namespace LocationModule.Domain.Aggregates;

public class TrackedUserPair
{
    public required Guid FirstUserId { get; init; }
    public required Guid SecondUserId { get; init; }
    public required DateTimeOffset ExpiresAt { get; init; }
}
