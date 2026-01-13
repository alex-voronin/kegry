using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Shared.Infrastructure.Storage;
using SharedCore.Api;
using SharedCore.Api.Dto;
using SharedCore.Extensions;
using UserProfileModule.API.Dto;
using UserProfileModule.Contracts.Events;
using UserProfileModule.Domain.Aggregates;
using UserProfileModule.Domain.Exceptions;
using UserProfileModule.Domain.Repositories;
using UserProfileModule.Domain.Services;

namespace UserProfileModule.API;

public class UserProfileEndpoints : IEndpoints
{
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var apiGroup = app.MapGroup("")
            .WithTags("User Profile API")
            .RequireAuthorization();
        apiGroup.MapPost("/api/v1/user-profile", CreateUserProfile);
        apiGroup.MapPatch("/api/v1/user-profile", PatchUserProfile);
        apiGroup.MapGet("/api/v1/user-profile/{id:guid?}", GetUserProfile);

        // Presigned URLs are better obtained via POST requests because this is semantically correct:
        // we create a new resource (so, these requests aren't idempotent),
        // and it helps avoid caching at the browser and CDN levels.
        apiGroup.MapPost("/api/v1/user-profile/avatar/upload-url", GetAvatarUploadUrl);
        apiGroup.MapPost("/api/v1/user-profile/cover/upload-url", GetCoverUploadUrl);
    }

    private static async Task<ApiResult.EmptyResult> CreateUserProfile(
        [FromBody] CreateUserProfileRequest request,
        ClaimsPrincipal user,
        IUserProfileRepository profileRepository,
        IHobbyService hobbyService,
        IS3StorageService storageService,
        CancellationToken ct)
    {
        var userId = user.GetId();
        var hobbyStubs = await hobbyService.BuildHobbyStubs(request.HobbyIds, ct);
        var profile = new User
        {
            Id = userId,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Bio = request.Bio,
            AvatarImageS3Key = storageService.PublicFileUrlToS3Key(request.AvatarImageUrl),
            CoverImageS3Key = storageService.PublicFileUrlToS3Key(request.CoverImageUrl),
            Birthday = request.Birthday,
            Gender = request.Gender,
            Hobbies = hobbyStubs
        };
        await profileRepository.AddAsync(profile, ct);

        var @event = BuildUserChangedEvent(user, profile);
        await profileRepository.PublishEventAsync(@event);
        await profileRepository.SaveChangesAndFlushMessagesAsync(ct);

        return ApiResult.Empty;
    }

    private static async Task<ApiResult.EmptyResult> PatchUserProfile(
        [FromBody] PatchUserProfileRequest request,
        ClaimsPrincipal user,
        IUserProfileRepository profileRepository,
        IHobbyService hobbyService,
        IS3StorageService storageService,
        CancellationToken ct)
    {
        var userId = user.GetId();
        var userProfile = await profileRepository.GetByIdAsync(userId, ct: ct);
        if (userProfile == null)
        {
            throw UserProfileExceptions.ProfileNotFound(userId);
        }

        userProfile.FirstName = request.FirstName ?? userProfile.FirstName;
        userProfile.LastName = request.LastName ?? userProfile.LastName;
        userProfile.Bio = request.Bio ?? userProfile.Bio;
        userProfile.Birthday = request.Birthday ?? userProfile.Birthday;
        userProfile.Gender = request.Gender ?? userProfile.Gender;

        if (request.AvatarImageUrl != null)
        {
            userProfile.AvatarImageS3Key = storageService.PublicFileUrlToS3Key(request.AvatarImageUrl);
        }

        if (request.CoverImageUrl != null)
        {
            userProfile.CoverImageS3Key = storageService.PublicFileUrlToS3Key(request.CoverImageUrl);
        }

        if (request.HobbyIds != null)
        {
            var hobbyStubs = await hobbyService.BuildHobbyStubs(request.HobbyIds, ct);
            userProfile.Hobbies = hobbyStubs;
        }

        var @event = BuildUserChangedEvent(user, userProfile);
        await profileRepository.PublishEventAsync(@event);
        await profileRepository.SaveChangesAndFlushMessagesAsync(ct);

        return ApiResult.Empty;
    }

    private static async Task<ApiResult<GetUserProfileResponse>> GetUserProfile(
        [FromQuery] Guid? id,
        ClaimsPrincipal user,
        IUserProfileRepository profileRepository,
        IS3StorageService storageService,
        CancellationToken ct)
    {
        var userId = id ?? user.GetId();
        var profile = await profileRepository.GetByIdAsync(
            id: userId,
            asNoTracking: true,
            includes: [p => p.Hobbies],
            ct: ct);

        if (profile == null)
        {
            throw UserProfileExceptions.ProfileNotFound(userId);
        }

        var dto = new UserProfileDto(
            FirstName: profile.FirstName,
            LastName: profile.LastName,
            Bio: profile.Bio,
            AvatarImageUrl: storageService.S3KeyToPublicFileUrl(profile.AvatarImageS3Key),
            CoverImageUrl: storageService.S3KeyToPublicFileUrl(profile.CoverImageS3Key),
            Birthday: profile.Birthday,
            Gender: profile.Gender,
            Hobbies: profile.Hobbies.Select(h => new HobbyDto(Id: h.Id, Name: h.Name)).ToArray()
        );

        return ApiResult<GetUserProfileResponse>.Build(new GetUserProfileResponse(dto));
    }

    private static async Task<ApiResult<GetPresignedUrlResponse>> GetAvatarUploadUrl(
        ClaimsPrincipal user,
        IProfileImageService imageService,
        CancellationToken ct)
    {
        var userId = user.GetId();
        var urls = await imageService.GetAvatarUploadUrlAsync(userId, ct);
        return ApiResult<GetPresignedUrlResponse>.Build(
            new GetPresignedUrlResponse(UploadUrl: urls.UploadUrl, PublicDownloadUrl: urls.PublicDownloadUrl)
        );
    }

    private static async Task<ApiResult<GetPresignedUrlResponse>> GetCoverUploadUrl(
        ClaimsPrincipal user,
        IProfileImageService imageService,
        CancellationToken ct)
    {
        var userId = user.GetId();
        var urls = await imageService.GetCoverUploadUrlAsync(userId, ct);
        return ApiResult<GetPresignedUrlResponse>.Build(
            new GetPresignedUrlResponse(UploadUrl: urls.UploadUrl, PublicDownloadUrl: urls.PublicDownloadUrl)
        );
    }

    private static UserProfileChangedEvent BuildUserChangedEvent(ClaimsPrincipal user, User profile)
    {
        return new UserProfileChangedEvent
        {
            UserId = user.GetId(),
            Email = user.GetEmail(),
            FirstName = profile.FirstName,
            LastName = profile.LastName,
            AvatarImageS3Key = profile.AvatarImageS3Key
        };
    }
}