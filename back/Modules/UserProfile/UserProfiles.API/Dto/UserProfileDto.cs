using SharedCore.ValueObjects;

namespace UserProfiles.API.Dto;

public class UserProfileDto
{
    public string FirstName { get; init; } = null!;
    public string LastName { get; init; } = null!;
    public string Bio { get; init; } = null!;
    public string AvatarImageUrl { get; init; } = null!;
    public string CoverImageUrl { get; init; } = null!;
    public DateOnly Birthday { get; init; }
    public Gender Gender { get; init; }
    public HobbyDto[] Hobbies { get; init; } = [];
}