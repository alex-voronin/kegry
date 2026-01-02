namespace FeedModule.API.Dto;

public class HobbyDto
{
    public required Guid Id { get; init; }
    public required string Name { get; init; } = null!;
}