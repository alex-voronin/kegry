using SharedCore.ValueObjects;

namespace UserProfileModule.API.Dto;

public record CreateUserProfileRequest(
    Guid? ReferrerId,
    string FirstName,
    string LastName,
    string Bio,
    string AvatarImageUrl,
    string CoverImageUrl,
    DateOnly Birthday,
    Gender Gender,
    Guid[] HobbyIds);