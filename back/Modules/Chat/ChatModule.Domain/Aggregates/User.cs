namespace ChatModule.Domain.Aggregates;

public class User
{
    public required Guid Id { get; init; }
    public required string FirstName { get; set; } = null!;
    public required string LastName { get; set; } = null!;
    public required string AvatarImageS3Key { get; set; } = null!;

    public ActiveChatUserLink? ChatLink { get; set; }
}