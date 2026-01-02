namespace FeedModule.API.Dto;

public class UserProfileDto
{
    public required Guid Id { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public required decimal? Rating { get; init; }
    public required int DistanceMeters { get; init; }
    public required HobbyDto[] Hobbies { get; init; }
}