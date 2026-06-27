namespace EventModule.Domain.Aggregates.ReadOnly;

public class FeedUserRelation
{
    public Guid Id { get; init; }
    public Guid MyUserId { get; init; }
    public Guid OtherUserId { get; init; }
    public bool IsBlocked { get; init; }
}
