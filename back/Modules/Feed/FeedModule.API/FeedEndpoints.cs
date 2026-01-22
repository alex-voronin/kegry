using System.Security.Claims;
using FeedModule.API.Dto;
using FeedModule.Domain.Repositories;
using FeedModule.Domain.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Shared.Infrastructure.Storage;
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
        apiGroup.MapGet("/api/v1/feed/status", GetFeedStatus);
        apiGroup.MapPost("/api/v1/feed/{id:guid}/rating", PostUserRating);
        apiGroup.MapPost("/api/v1/feed/{id:guid}/invite", InviteUser);
        apiGroup.MapPost("/api/v1/feed/{id:guid}/skip", SkipUser);
        apiGroup.MapPost("/api/v1/feed/{id:guid}/block", BlockUser);
        apiGroup.MapPost("/api/v1/feed/{id:guid}/ignore", IgnoreUser);
        apiGroup.MapGet("/api/v1/feed/filter", GetFilter);
        apiGroup.MapPut("/api/v1/feed/filter", UpdateFilter);
    }

    private static async Task<ApiResult<GetFeedResponse>> GetFeed(
        ClaimsPrincipal user,
        IFeedScoringService feedScoringService,
        IS3StorageService storageService,
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
                    CoverImageUrl: storageService.S3KeyToPublicFileUrl(f.User.CoverImageS3Key),
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

    private static async Task<ApiResult<GetFeedStatusResponse>> GetFeedStatus(
        ClaimsPrincipal user,
        IFeedUserRelationRepository repository,
        CancellationToken ct)
    {
        var (isInMeeting, userIdToRate) = await repository.GetFeedStatusAsync(user.GetId(), ct);
        return ApiResult<GetFeedStatusResponse>.Build(
            new GetFeedStatusResponse(isInMeeting, userIdToRate)
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

    private static async Task<ApiResult.EmptyResult> IgnoreUser(
        [FromRoute] Guid id,
        ClaimsPrincipal user,
        IUserActionsService userActionsService,
        CancellationToken ct)
    {
        await userActionsService.IgnoreUserAsync(user.GetId(), id, ct);
        return ApiResult.Empty;
    }

    private static async Task<ApiResult<GetFilterResponse>> GetFilter(
        ClaimsPrincipal user,
        IUserActionsService userActionsService,
        CancellationToken ct)
    {
        var filter = await userActionsService.GetFilterAsync(user.GetId(), ct);
        var dto = new FilterDto(
            RadiusMeters: filter.RadiusMeters,
            AgeFrom: filter.AgeFrom,
            AgeTo: filter.AgeTo,
            Gender: filter.Gender,
            HobbyIds: filter.HobbyIds);

        return ApiResult<GetFilterResponse>.Build(new GetFilterResponse(dto));
    }

    private static async Task<ApiResult.EmptyResult> UpdateFilter(
        [FromBody] UpdateFilterRequest request,
        ClaimsPrincipal user,
        IUserActionsService userActionsService,
        CancellationToken ct)
    {
        var domainFilter = new Domain.Aggregates.Filter
        {
            UserId = user.GetId(),
            RadiusMeters = request.Filter.RadiusMeters,
            AgeFrom = request.Filter.AgeFrom,
            AgeTo = request.Filter.AgeTo,
            Gender = request.Filter.Gender,
            HobbyIds = request.Filter.HobbyIds
        };

        await userActionsService.UpdateFilterAsync(user.GetId(), domainFilter, ct);
        return ApiResult.Empty;
    }
}