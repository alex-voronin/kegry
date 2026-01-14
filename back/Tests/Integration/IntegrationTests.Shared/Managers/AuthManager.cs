using IntegrationTests.Shared.HttpClients;
using LocationModule.API.Dto;
using Microsoft.Extensions.DependencyInjection;
using SecurityModule.API.Dto;
using SecurityModule.Domain.Aggregates;
using SecurityModule.Infrastructure.Persistence;
using SharedCore.Api.Dto;
using SharedCore.ValueObjects;
using Shouldly;
using UserProfileModule.API.Dto;

namespace IntegrationTests.Shared.Managers;

public class AuthManager
{
    private readonly IntegrationApiClient _apiClient;
    private readonly IServiceProvider _serviceProvider;

    public AuthManager(IntegrationApiClient apiClient, IServiceProvider serviceProvider)
    {
        _apiClient = apiClient;
        _serviceProvider = serviceProvider;
    }

    public async Task<AuthResponse> CreateAndLoginUserAsync(string email, string password)
    {
        // 1. Sign up
        var signupRequest = new SignUpRequest(email, password);
        var signupResponse = await _apiClient.PostAsync<SignUpRequest, ApiResult<AuthResponse>>(
            ApiRoutes.Auth.Signup, signupRequest);

        signupResponse.Success.ShouldBeTrue();
        var userId = Guid.Parse(signupResponse.Result.UserId);

        // 2. Manually verify user in DB
        using (var scope = _serviceProvider.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<SecurityContext>();
            var user = await context.Users.FindAsync(userId);
            user.ShouldNotBeNull();
            user.RequiredAction = RequiredAction.None;
            user.EmailConfirmed = true;
            await context.SaveChangesAsync();
        }

        // 3. Sign in again to get access token
        var signinRequest = new SignInRequest(email, password);
        var signinResponse = await _apiClient.PostAsync<SignInRequest, ApiResult<SigninResponse>>(
            ApiRoutes.Auth.Signin, signinRequest);

        signinResponse.Success.ShouldBeTrue(
            $"Login failed with errors: {string.Join(", ", signinResponse.Errors.Select(e => e.Error))}");
        signinResponse.Result.Auth.ShouldNotBeNull();
        signinResponse.Result.Auth.AccessToken.ShouldNotBeNullOrEmpty();

        return signinResponse.Result.Auth;
    }

    public async Task CreateDefaultProfileAndSubmitLocation()
    {
        await _apiClient.PostAsync<CreateUserProfileRequest, ApiResult.EmptyResult>(ApiRoutes.UserProfile.Profile,
            new CreateUserProfileRequest(
                FirstName: "FirstName",
                LastName: "LastName",
                Bio: "Bio",
                AvatarImageUrl: TestConfig.S3.PublicUrl + "/avatar.jpg",
                CoverImageUrl: TestConfig.S3.PublicUrl + "/cover-image.jpg",
                Birthday: new DateOnly(1980, 1, 1),
                Gender: Gender.Male,
                HobbyIds: []
            )
        );

        var response = await _apiClient.PostAsync<UpdateLocationRequest, ApiResult.EmptyResult>(ApiRoutes.Location.PostLocation,
            new UpdateLocationRequest(Latitude: 0.0, Longitude: 0.0));
        response.Success.ShouldBeTrue();
    }
}