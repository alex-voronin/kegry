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
        apiGroup.MapPost("/api/v1/feed/{id:guid}/accept-invite", AcceptInviteUser);
        apiGroup.MapGet("/api/v1/feed/{id:guid}/invite-expiration", GetInviteExpiration);
        apiGroup.MapPost("/api/v1/feed/{id:guid}/skip", SkipUser);
        apiGroup.MapPost("/api/v1/feed/{id:guid}/block", BlockUser);
        apiGroup.MapPost("/api/v1/feed/{id:guid}/unblock", UnblockUser);
        apiGroup.MapPost("/api/v1/feed/{id:guid}/ignore", IgnoreUser);
        apiGroup.MapGet("/api/v1/feed/filter", GetFilter);
        apiGroup.MapPut("/api/v1/feed/filter", UpdateFilter);
        apiGroup.MapGet("/api/v1/feed/blocked-users", GetBlockedUsers);
        apiGroup.MapGet("/api/v1/feed/friends", GetFriends);
        apiGroup.MapPost("/api/v1/feed/friends/{id:guid}/invite", InviteFriend);
        apiGroup.MapPost("/api/v1/feed/{id:guid}/remove-friend", RemoveFriend);
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
                    CoverImageUrl: storageService.S3KeyToPublicFileUrl(f.User.CoverImageS3Key, f.User.ChangedAt),
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
        await userActionsService.InviteUserAsync(
            myUserId: user.GetId(),
            otherUserId: id,
            expiresAt: request.ExpiresAt,
            ct: ct);
        return ApiResult.Empty;
    }

    private static async Task<ApiResult.EmptyResult> InviteFriend(
        [FromRoute] Guid id,
        [FromBody] InviteUserRequest request,
        ClaimsPrincipal user,
        IUserActionsService userActionsService,
        CancellationToken ct)
    {
        await userActionsService.InviteFriendAsync(
            myUserId: user.GetId(),
            otherUserId: id,
            expiresAt: request.ExpiresAt,
            ct: ct);
        return ApiResult.Empty;
    }

    private static async Task<ApiResult.EmptyResult> AcceptInviteUser(
        [FromRoute] Guid id,
        ClaimsPrincipal user,
        IUserActionsService userActionsService,
        CancellationToken ct)
    {
        await userActionsService.AcceptInviteAsync(user.GetId(), id, ct);
        return ApiResult.Empty;
    }

    private static async Task<ApiResult<GetInviteExpirationResponse>> GetInviteExpiration(
        [FromRoute] Guid id,
        ClaimsPrincipal user,
        IFeedUserRelationRepository repository,
        TimeProvider timeProvider,
        CancellationToken ct)
    {
        var relation = await repository.GetAsync(id, user.GetId(), asNoTracking: true, ct);
        DateTimeOffset? expiresAt = null;
        if (relation != null &&
            relation.InvitedAt != DateTimeOffset.MinValue &&
            !relation.IsInviteExpired(timeProvider.GetUtcNow()))
        {
            expiresAt = relation.InviteExpiresAt;
        }

        return ApiResult<GetInviteExpirationResponse>.Build(
            new GetInviteExpirationResponse(expiresAt));
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
        var domainFilter = new Domain.Aggregates.Filter(
            user.GetId(),
            request.Filter.RadiusMeters,
            request.Filter.AgeFrom,
            request.Filter.AgeTo,
            request.Filter.Gender,
            request.Filter.HobbyIds);

        await userActionsService.UpdateFilterAsync(domainFilter, ct);
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

    private static async Task<ApiResult<PaginatedResponse<FriendUserDto>>> GetFriends(
        [AsParameters] PaginatedRequest request,
        ClaimsPrincipal user,
        IUserRepository repository,
        IS3StorageService storageService,
        CancellationToken ct)
    {
        var friendsPage = await repository.GetFriendsAsync(user.GetId(), request.Offset, request.Limit, ct);
        var friendsCount = await repository.GetFriendsCount(user.GetId(), ct);
        var dtos = friendsPage
            .Select(u => DomainUserToFriendUserDto(u, storageService))
            .ToArray();
        return ApiResult<PaginatedResponse<FriendUserDto>>.Build(
            new PaginatedResponse<FriendUserDto>(
                Data: dtos,
                TotalCount: friendsCount
            )
        );
    }

    private static async Task<ApiResult.EmptyResult> RemoveFriend(
        [FromRoute] Guid id,
        ClaimsPrincipal user,
        IUserActionsService userActionsService,
        CancellationToken ct)
    {
        await userActionsService.RemoveFriendAsync(user.GetId(), id, ct);
        return ApiResult.Empty;
    }

    private static BlockedUserDto DomainUserToBlockedUserDto(User user, IS3StorageService storageService)
    {
        return new BlockedUserDto(
            Id: user.Id,
            FirstName: user.FirstName,
            LastName: user.LastName,
            AvatarUrl: storageService.S3KeyToPublicFileUrl(user.AvatarImageS3Key, user.ChangedAt)
        );
    }

    private static FriendUserDto DomainUserToFriendUserDto(User user, IS3StorageService storageService)
    {
        return new FriendUserDto(
            Id: user.Id,
            FirstName: user.FirstName,
            LastName: user.LastName,
            AvatarUrl: storageService.S3KeyToPublicFileUrl(user.AvatarImageS3Key, user.ChangedAt)
        );
    }
}