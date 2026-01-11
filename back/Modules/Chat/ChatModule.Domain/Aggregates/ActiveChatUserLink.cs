namespace ChatModule.Domain.Aggregates;

public class ActiveChatUserLink
{
    public required Guid UserId { get; init; }
    public required Guid ChatId { get; init; }

    public User User { get; init; } = null!;
    public Chat Chat { get; init; } = null!;
}