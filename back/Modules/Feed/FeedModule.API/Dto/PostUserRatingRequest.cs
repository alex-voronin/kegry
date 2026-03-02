using Microsoft.AspNetCore.Mvc;

namespace FeedModule.API.Dto;

public record PostUserRatingRequest([FromRoute] Guid Id, [FromBody] UserRatingDto RatingDto);