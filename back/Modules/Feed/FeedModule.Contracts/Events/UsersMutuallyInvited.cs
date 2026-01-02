namespace FeedModule.Contracts.Events;

public class UsersMutuallyInvited
{
    public required Guid User1Id { get; set; }
    public required Guid User2Id { get; set; }
}