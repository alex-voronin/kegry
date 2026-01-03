namespace FeedModule.Contracts.Events;

public class UsersMutuallyInvited
{
    public required Guid FirstUserId { get; init; }
    public required string FirstUserFullName { get; init; }
    public required Guid SecondUserId { get; init; }
    public required string SecondUserFullName { get; init; }
}