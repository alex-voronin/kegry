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
        apiGroup.MapPost("/api/v1/user-profile/privacy", UpdatePrivacy);
        apiGroup.MapGet("/api/v1/user-profile/{id:guid?}", GetUserProfile);

        // Presigned URLs are better obtained via POST requests because this is semantically correct:
        // we create a new resource (so, these requests aren't idempotent),
        // and it helps avoid caching at the browser and CDN levels.
        apiGroup.MapPost("/api/v1/user-profile/avatar/upload-url", GetAvatarUploadUrl);
        apiGroup.MapPost("/api/v1/user-profile/cover/upload-url", GetCoverUploadUrl);
        apiGroup.MapPost("/api/v1/user-profiles", GetUserProfilesBatch);
    }

    private static async Task<ApiResult.EmptyResult> CreateUserProfile(
        [FromBody] CreateUserProfileRequest request,
        ClaimsPrincipal user,
        IUserRepository repository,
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
        await repository.AddAsync(profile, ct);

        var @event = BuildUserChangedEvent(
            user: user,
            profile: profile,
            isNewProfile: true,
            referrerId: request.ReferrerId);
        await repository.PublishEventAsync(@event);
        await repository.SaveChangesAndFlushMessagesAsync(ct);

        return ApiResult.Empty;
    }

    private static async Task<ApiResult.EmptyResult> PatchUserProfile(
        [FromBody] PatchUserProfileRequest request,
        ClaimsPrincipal user,
        IUserRepository repository,
        IHobbyService hobbyService,
        IS3StorageService storageService,
        CancellationToken ct)
    {
        var userId = user.GetId();
        var userProfile = await repository.GetByIdAsync(userId, includes: [u => u.Hobbies], ct: ct);
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
            var contextHobbies = userProfile.Hobbies
                .Where(h => request.HobbyIds.Contains(h.Id)).
                ToArray();
            var contextHobbyIds = contextHobbies
                .Select(h => h.Id)
                .ToHashSet();
            var nonContextHobbyIds = request.HobbyIds
                .Where(id => !contextHobbyIds.Contains(id))
                .ToArray();
            var nonContextStubs = await hobbyService.BuildHobbyStubs(nonContextHobbyIds, ct);
            var newHobbies = contextHobbies.Concat(nonContextStubs).ToArray();
            userProfile.Hobbies = newHobbies;
        }

        var @event = BuildUserChangedEvent(
            user: user,
            profile: userProfile,
            isNewProfile: true,
            referrerId: null);
        await repository.PublishEventAsync(@event);
        await repository.SaveChangesAndFlushMessagesAsync(ct);

        return ApiResult.Empty;
    }

    private static async Task<ApiResult<GetUserProfileResponse>> GetUserProfile(
        [FromRoute] Guid? id,
        ClaimsPrincipal user,
        IUserRepository repository,
        IS3StorageService storageService,
        CancellationToken ct)
    {
        var userId = id ?? user.GetId();
        var profile = await repository.GetByIdAsync(
            id: userId,
            asNoTracking: true,
            includes: [p => p.Hobbies],
            ct: ct);

        if (profile == null)
        {
            throw UserProfileExceptions.ProfileNotFound(userId);
        }

        var dto = new UserProfileDto(
            Id: profile.Id.ToString(),
            IsPublic: profile.IsPublic,
            FirstName: profile.FirstName,
            LastName: profile.LastName,
            Bio: profile.Bio,
            AvatarImageUrl: storageService.S3KeyToPublicFileUrl(profile.AvatarImageS3Key, profile.UpdatedAt),
            CoverImageUrl: storageService.S3KeyToPublicFileUrl(profile.CoverImageS3Key, profile.UpdatedAt),
            Birthday: profile.Birthday,
            Gender: profile.Gender,
            Hobbies: profile.Hobbies
                .OrderBy(h => h.Name)
                .Select(h => new HobbyDto(Id: h.Id, Name: h.Name))
                .ToArray()
        );

        return ApiResult<GetUserProfileResponse>.Build(new GetUserProfileResponse(dto));
    }

    private static async Task<ApiResult<UserProfileDto[]>> GetUserProfilesBatch(
        [FromBody] Guid[] ids,
        IUserRepository repository,
        IS3StorageService storageService,
        CancellationToken ct)
    {
        var users = await repository.GetUsersByIdsAsync(ids, ct);

        var dtos = users.Select(u => new UserProfileDto(
            Id: u.Id.ToString(),
            IsPublic: u.IsPublic,
            FirstName: u.FirstName,
            LastName: u.LastName,
            Bio: u.Bio,
            AvatarImageUrl: u.AvatarImageS3Key != null
                ? storageService.S3KeyToPublicFileUrl(u.AvatarImageS3Key, u.UpdatedAt)
                : null,
            CoverImageUrl: u.CoverImageS3Key != null
                ? storageService.S3KeyToPublicFileUrl(u.CoverImageS3Key, u.UpdatedAt)
                : null,
            Birthday: u.Birthday,
            Gender: u.Gender,
            Hobbies: u.Hobbies
                .OrderBy(h => h.Name)
                .Select(h => new HobbyDto(Id: h.Id, Name: h.Name))
                .ToArray()
        )).ToArray();

        return ApiResult<UserProfileDto[]>.Build(dtos);
    }

    private static async Task<ApiResult.EmptyResult> UpdatePrivacy(
        [FromBody] UpdatePrivacyRequest request,
        ClaimsPrincipal user,
        IUserRepository repository,
        CancellationToken ct)
    {
        var userId = user.GetId();
        var userProfile = await repository.GetByIdAsync(userId, ct: ct);
        if (userProfile == null)
        {
            throw UserProfileExceptions.ProfileNotFound(userId);
        }

        userProfile.IsPublic = request.IsPublic;
        await repository.SaveChangesAsync(ct);

        return ApiResult.Empty;
    }

    private static async Task<ApiResult<GetPresignedUrlResponse>> GetAvatarUploadUrl(
        [FromBody] GetPresignedUrlRequest request,
        ClaimsPrincipal user,
        IProfileImageService imageService,
        CancellationToken ct)
    {
        Validation.ThrowIfFileSizeExceed(request.ContentLength, Validation.MaxAvatarSizeBytes);
        var userId = user.GetId();
        var urls = await imageService.GetAvatarUploadUrlAsync(
            userId: userId,
            contentType: request.ContentType,
            contentLength: request.ContentLength,
            ct: ct);
        return ApiResult<GetPresignedUrlResponse>.Build(
            new GetPresignedUrlResponse(UploadUrl: urls.UploadUrl, PublicDownloadUrl: urls.PublicDownloadUrl)
        );
    }

    private static async Task<ApiResult<GetPresignedUrlResponse>> GetCoverUploadUrl(
        [FromBody] GetPresignedUrlRequest request,
        ClaimsPrincipal user,
        IProfileImageService imageService,
        CancellationToken ct)
    {
        Validation.ThrowIfFileSizeExceed(request.ContentLength, Validation.MaxCoverSizeBytes);
        var userId = user.GetId();
        var urls = await imageService.GetCoverUploadUrlAsync(userId: userId,
            contentType: request.ContentType,
            contentLength: request.ContentLength,
            ct: ct);
        return ApiResult<GetPresignedUrlResponse>.Build(
            new GetPresignedUrlResponse(UploadUrl: urls.UploadUrl, PublicDownloadUrl: urls.PublicDownloadUrl)
        );
    }

    private static UserProfileChangedEvent BuildUserChangedEvent(ClaimsPrincipal user,
        User profile,
        bool isNewProfile,
        Guid? referrerId)

    {
        return new UserProfileChangedEvent
        {
            IsNewProfile = isNewProfile,
            ReferrerId = referrerId,
            UserId = user.GetId(),
            Email = user.GetEmail(),
            FirstName = profile.FirstName,
            LastName = profile.LastName,
            AvatarImageS3Key = profile.AvatarImageS3Key,
            UpdatedAt = profile.UpdatedAt,
        };
    }
}