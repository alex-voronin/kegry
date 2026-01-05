using System.Security.Claims;
using FeedModule.API.Dto;
using FeedModule.Application.Services;
using FeedModule.Domain.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using SharedCore.Api;
using SharedCore.Api.Dto;
using SharedCore.Extensions;

namespace FeedModule.API;

public class FeedEndpoints : IEndpoints
{
    public static void MapEndpoints(IEndpointRouteBuilder builder)
    {
        var apiGroup = builder.MapGroup("")
            .WithTags("Feed API")
            .RequireAuthorization();
        apiGroup.MapGet("/api/v1/feed", GetFeed);
        apiGroup.MapPost("/api/v1/feed/{id:guid}/rating", PostUserRating);
        apiGroup.MapPost("/api/v1/feed/{id:guid}/invite", InviteUser);
        apiGroup.MapPost("/api/v1/feed/{id:guid}/skip", SkipUser);
        apiGroup.MapPost("/api/v1/feed/{id:guid}/block", BlockUser);
    }

    private static async Task<ApiResult<GetFeedResponse>> GetFeed(
        ClaimsPrincipal user,
        IFeedScoringService feedScoringService,
        CancellationToken ct)
    {
        var feed = await feedScoringService.GetFeedAsync(user.GetId(), ct: ct);
        var dtos = feed.Select(f =>
                new UserProfileDto(
                    Id: f.User.Id,
                    FirstName: f.User.FirstName,
                    LastName: f.User.LastName,
                    Rating: f.Rating,
                    DistanceMeters: f.DistanceMeters,
                    Hobbies: f.User.Hobbies.Select(h =>
                            new HobbyDto
                            (
                                Id: h.Id,
                                Name: h.Name
                            )
                        )
                        .ToArray()
                )
            )
            .ToArray();

        return ApiResult<GetFeedResponse>.Build(
            new GetFeedResponse(Feed: dtos)
        );
    }

    private static async Task<ApiResult.EmptyResult> PostUserRating(
        [AsParameters] PostUserRatingRequest request,
        ClaimsPrincipal user,
        IUserActionsService userActionsService,
        CancellationToken ct)
    {
        await userActionsService.RateUserAsync(user.GetId(), request.Id, request.Rating.Value, ct);
        return ApiResult.Empty;
    }

    private static async Task<ApiResult.EmptyResult> BlockUser(
        [FromRoute] Guid id,
        ClaimsPrincipal user,
        IUserActionsService userActionsService,
        CancellationToken ct)
    {
        await userActionsService.BlockUserAsync(user.GetId(), id, ct);
        return ApiResult.Empty;
    }

    private static async Task<ApiResult.EmptyResult> SkipUser(
        [FromRoute] Guid id,
        ClaimsPrincipal user,
        IUserActionsService userActionsService,
        CancellationToken ct)
    {
        await userActionsService.SkipUserAsync(user.GetId(), id, ct);
        return ApiResult.Empty;
    }

    private static async Task<ApiResult.EmptyResult> InviteUser(
        [FromRoute] Guid id,
        ClaimsPrincipal user,
        IUserActionsService userActionsService,
        CancellationToken ct)
    {
        await userActionsService.InviteUserAsync(user.GetId(), id, ct);
        return ApiResult.Empty;
    }
}