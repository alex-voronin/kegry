namespace FeedModule.API.Dto;

public record UserProfileDto(
    Guid Id,
    string FirstName,
    string LastName,
    decimal? Rating,
    int DistanceMeters,
    string CoverImageUrl,
    HobbyDto[] Hobbies
);