namespace FeedModule.API.Dto;

public class GetFeedResponse
{
    public required UserProfileDto[] Feed { get; init; }
}