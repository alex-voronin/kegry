using SharedCore.ValueObjects;

namespace UserProfileModule.API.Dto;

public record UpdateUserProfileRequest(
    string FirstName,
    string LastName,
    string Bio,
    string AvatarImageUrl,
    string CoverImageUrl,
    DateOnly Birthday,
    Gender Gender,
    Guid[] HobbyIds);