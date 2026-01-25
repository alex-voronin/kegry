using SharedCore.ValueObjects;

namespace UserProfileModule.API.Dto;

public record UserProfileDto(
    string Id,
    string FirstName,
    string LastName,
    string Bio,
    string AvatarImageUrl,
    string CoverImageUrl,
    DateOnly Birthday,
    Gender Gender,
    HobbyDto[] Hobbies);