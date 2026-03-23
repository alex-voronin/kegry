using ChatModule.Domain.Entities;
using SharedCore.Interfaces;

namespace ChatModule.Domain.Aggregates;

public class User : ISoftDeletable<Guid>
{
    public required Guid Id { get; init; }
    public bool IsDeleted { get; set; }
    public required string FirstName { get; set; } = null!;
    public required string LastName { get; set; } = null!;
    public required string AvatarImageS3Key { get; set; } = null!;
    public required DateTimeOffset UpdatedAt { get; set; }

    public ActiveChatUserLink? ChatLink { get; set; }
}