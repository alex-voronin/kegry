using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using SharedCore.Api;
using SharedCore.Api.Dto;
using SharedCore.Extensions;
using UserProfileModule.API.Dto;
using UserProfileModule.Domain.Aggregates;
using UserProfileModule.Domain.Exceptions;

namespace UserProfileModule.API;

public class UserProfileEndpoints : IEndpoints
{
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var apiGroup = app.MapGroup("")
            .WithTags("User Profile API")
            .RequireAuthorization(); 
        apiGroup.MapPost("/api/v1/user-profile", CreateUserProfile);
        apiGroup.MapPut("/api/v1/user-profile", UpdateUserProfile);
        apiGroup.MapGet("/api/v1/user-profile/{id:guid?}", GetUserProfile);
    }

    private static async Task<ApiResult.EmptyResult> CreateUserProfile(
        [FromBody] CreateUserProfileRequest request,
        ClaimsPrincipal user,
        IUserProfileRepository profileRepository,
        CancellationToken ct)
    {
        var userId = user.GetId();

        var hobbyStubs = request.HobbyIds
            .Select(id => new Hobby { Id = id })
            .ToArray();

        var profile = new UserProfile
        {
            Id = userId,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Bio = request.Bio,
            AvatarImageUrl = request.AvatarImageUrl,
            CoverImageUrl = request.CoverImageUrl,
            Birthday = request.Birthday,
            Gender = request.Gender,
            Hobbies = hobbyStubs
        };
        await profileRepository.SaveAsync(profile, ct);

        return ApiResult.Empty;
    }

    private static async Task<ApiResult.EmptyResult> UpdateUserProfile(
        [FromBody] UpdateUserProfileRequest request,
        ClaimsPrincipal user,
        IUserProfileRepository profileRepository,
        CancellationToken ct)
    {
        var userId = user.GetId();
        var userProfile = await profileRepository.GetByIdAsync(userId, ct: ct);
        if (userProfile == null)
        {
            throw UserProfileExceptions.ProfileNotFound(userId);
        }

        var hobbyStubs = request.HobbyIds
            .Select(id => new Hobby { Id = id })
            .ToArray();

        userProfile.FirstName = request.FirstName;
        userProfile.LastName = request.LastName;
        userProfile.Bio = request.Bio;
        userProfile.AvatarImageUrl = request.AvatarImageUrl;
        userProfile.CoverImageUrl = request.CoverImageUrl;
        userProfile.Birthday = request.Birthday;
        userProfile.Gender = request.Gender;
        userProfile.Hobbies = hobbyStubs;

        await profileRepository.SaveAsync(userProfile, ct);
        return ApiResult.Empty;
    }

    private static async Task<ApiResult<GetUserProfileResponse>> GetUserProfile(
        [FromQuery] Guid? id,
        ClaimsPrincipal user,
        IUserProfileRepository profileRepository,
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

        var dto = new UserProfileDto()
        {
            FirstName = profile.FirstName,
            LastName = profile.LastName,
            Bio = profile.Bio,
            AvatarImageUrl = profile.AvatarImageUrl,
            CoverImageUrl = profile.CoverImageUrl,
            Birthday = profile.Birthday,
            Gender = profile.Gender,
            Hobbies = profile.Hobbies.Select(h => new HobbyDto()
                {
                    Id = h.Id,
                    Name = h.Name,
                })
                .ToArray()
        };
        return ApiResult<GetUserProfileResponse>.Build(
            new GetUserProfileResponse
            {
                UserProfile = dto
            }
        );
    }
}