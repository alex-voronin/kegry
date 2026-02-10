using System.Security.Claims;
using FeedModule.API.Dto;
using FeedModule.Domain.Aggregates.ReadOnly;
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
        apiGroup.MapPost("/api/v1/feed/{id:guid}/unblock", UnblockUser);
        apiGroup.MapPost("/api/v1/feed/{id:guid}/ignore", IgnoreUser);
        apiGroup.MapGet("/api/v1/feed/filter", GetFilter);
        apiGroup.MapPut("/api/v1/feed/filter", UpdateFilter);
        apiGroup.MapGet("/api/v1/feed/blocked-users", GetBlockedUsers);
        apiGroup.MapGet("/api/v1/feed/settings", GetSettings);
        apiGroup.MapPut("/api/v1/feed/settings", UpdateSettings);
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
        IUserActionLimitsService userActionsLimitsService,
        CancellationToken ct)
    {
        var availableInvitesCount = await userActionsLimitsService.GetAvailableInvitesAsync(user.GetId(), ct);
        var (isInMeeting, userIdToRate) = await repository.GetFeedStatusAsync(user.GetId(), ct);
        return ApiResult<GetFeedStatusResponse>.Build(
            new GetFeedStatusResponse(isInMeeting, userIdToRate, availableInvitesCount)
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
        [FromBody] BlockUserRequest request,
        ClaimsPrincipal user,
        IUserActionsService userActionsService,
        CancellationToken ct)
    {
        await userActionsService.BlockUserAsync(user.GetId(), id, request.ReportMessage, ct);
        return ApiResult.Empty;
    }

    private static async Task<ApiResult.EmptyResult> UnblockUser(
        [FromRoute] Guid id,
        ClaimsPrincipal user,
        IUserActionsService userActionsService,
        CancellationToken ct)
    {
        await userActionsService.UnblockUserAsync(user.GetId(), id, ct);
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
        [FromBody] InviteUserRequest request,
        ClaimsPrincipal user,
        IUserActionsService userActionsService,
        CancellationToken ct)
    {
        await userActionsService.InviteUserAsync(user.GetId(), id, request.InviteExpiration, ct);
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

    private static async Task<ApiResult<PaginatedResponse<BlockedUserDto>>> GetBlockedUsers(
        [AsParameters] PaginatedRequest request,
        ClaimsPrincipal user,
        IUserRepository repository,
        IS3StorageService storageService,
        CancellationToken ct)
    {
        var blockedUsersPage = await repository.GetBlockedUsersAsync(user.GetId(), request.Offset, request.Limit, ct);
        var blockedUsersCount = await repository.GetBlockedUsersCount(user.GetId(), ct);
        var dtos = blockedUsersPage
            .Select(u => DomainUserToBlockedUserDto(u, storageService))
            .ToArray();
        return ApiResult<PaginatedResponse<BlockedUserDto>>.Build(
            new PaginatedResponse<BlockedUserDto>(
                Data: dtos,
                TotalCount: blockedUsersCount
            )
        );
    }

    private static async Task<ApiResult<GetSettingsResponse>> GetSettings(
        ClaimsPrincipal user,
        IDisplaySettingsRepository repository,
        CancellationToken ct)
    {
        var settings = await repository.GetByIdAsync(user.GetId(), ct: ct);
        if (settings == null)
        {
            return ApiResult<GetSettingsResponse>.Build(
                new GetSettingsResponse(
                    new SettingsDto(Domain.ValueObjects.DistanceUnits.Kilometers)
                )
            );
        }

        return ApiResult<GetSettingsResponse>.Build(
            new GetSettingsResponse(
                new SettingsDto(settings.DistanceUnits)
            )
        );
    }

    private static async Task<ApiResult.EmptyResult> UpdateSettings(
        [FromBody] UpdateSettingsRequest request,
        ClaimsPrincipal user,
        IDisplaySettingsRepository repository,
        CancellationToken ct)
    {
        var userId = user.GetId();
        var settings = await repository.GetByIdAsync(userId, ct: ct);

        if (settings == null)
        {
            settings = new Domain.Aggregates.DisplaySettings
            {
                UserId = userId,
                DistanceUnits = request.DistanceUnits
            };
            await repository.AddAsync(settings, ct);
        }
        else
        {
            settings.DistanceUnits = request.DistanceUnits;
        }
        await repository.SaveAsync(settings, ct);

        return ApiResult.Empty;
    }

    private static BlockedUserDto DomainUserToBlockedUserDto(User user, IS3StorageService storageService)
    {
        return new BlockedUserDto(
            Id: user.Id,
            FirstName: user.FirstName,
            LastName: user.LastName,
            AvatarUrl: storageService.S3KeyToPublicFileUrl(user.AvatarImageS3Key)
        );
    }
}