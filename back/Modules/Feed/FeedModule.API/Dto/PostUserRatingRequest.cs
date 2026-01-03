using Microsoft.AspNetCore.Mvc;

namespace FeedModule.API.Dto;

public class PostUserRatingRequest
{
    [FromRoute]
    public required Guid Id { get; init; }

    [FromBody]
    public required UserRatingDto Rating { get; init; }
}
