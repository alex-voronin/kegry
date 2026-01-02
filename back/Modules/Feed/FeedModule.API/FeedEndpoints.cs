using System.Security.Claims;
using FeedModule.API.Dto;
using FeedModule.Domain.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SharedCore.Api;
using SharedCore.Api.Dto;
using SharedCore.Extensions;

namespace FeedModule.API;

public class FeedEndpoints : IEndpoints
{
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/feed", GetFeed)
            .WithTags("Feed API")
            .RequireAuthorization();
    }

    private static async Task<ApiResult<GetFeedResponse>> GetFeed(
        ClaimsPrincipal user,
        IFeedScoringService feedScoringService,
        CancellationToken ct)
    {
        var feed = await feedScoringService.GetFeedAsync(user.GetId(), ct: ct);
        var dtos = feed.Select(f => new UserProfileDto
            {
                Id = f.User.Id,
                FirstName = f.User.FirstName,
                LastName = f.User.LastName,
                Rating = f.Rating,
                DistanceMeters = f.DistanceMeters,
                Hobbies = f.User.Hobbies.Select(h => new HobbyDto
                    {
                        Id = h.Id,
                        Name = h.Name
                    })
                    .ToArray()
            })
            .ToArray();
        
        return ApiResult<GetFeedResponse>.Build(
            new GetFeedResponse
            {
                Feed = dtos
            }
        );
    }
}