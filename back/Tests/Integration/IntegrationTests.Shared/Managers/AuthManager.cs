using IntegrationTests.Shared.HttpClients;
using LocationModule.API.Dto;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SecurityModule.API.Dto;
using SecurityModule.Domain.Aggregates;
using SecurityModule.Infrastructure.Persistence;
using SharedCore.Api.Dto;
using SharedCore.Extensions;
using SharedCore.ValueObjects;
using Shouldly;
using UserProfileModule.API.Dto;
using Wolverine.Tracking;

namespace IntegrationTests.Shared.Managers;

public class AuthManager
{
    private readonly IntegrationApiClient _apiClient;
    private readonly IServiceProvider _serviceProvider;
    private readonly IHost _host;

    public AuthManager(IntegrationApiClient apiClient, IServiceProvider serviceProvider, IHost host)
    {
        _apiClient = apiClient;
        _serviceProvider = serviceProvider;
        _host = host;
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

    public async Task CreateDefaultProfileAndSubmitLocation(Guid? refererId = null)
    {
        await _host.ExecuteAndWaitAsync(async () =>
        {
            await CreateProfile(refererId: refererId);
            await SubmitLocation();
        });
    }

    public async Task CreateProfile(
        Guid? refererId = null,
        string firstName = "Main",
        string lastName = "MainLastName",
        Gender gender = Gender.Male,
        DateOnly? birthday = null,
        Guid[]? hobbyIds = null)
    {
        hobbyIds ??= ["boxing".ToDeterministicGuid()];
        var response = await _apiClient.PostAsync<CreateUserProfileRequest, ApiResult.EmptyResult>(ApiRoutes.UserProfile.Profile,
            new CreateUserProfileRequest(
                ReferrerId: refererId,
                FirstName: firstName,
                LastName: lastName,
                Bio: "Bio",
                AvatarImageUrl: TestConfig.S3.PublicUrl + "/avatar.jpg",
                CoverImageUrl: TestConfig.S3.PublicUrl + "/cover-image.jpg",
                Birthday: birthday ?? new DateOnly(1980, 1, 1),
                Gender: gender,
                HobbyIds: hobbyIds ?? []
            )
        );
        response.Success.ShouldBeTrue();
    }

    public async Task SubmitLocation(double latitude = 0.0, double longitude = 0.0)
    {
        var response = await _apiClient.PostAsync<UpdateLocationRequest, ApiResult.EmptyResult>(ApiRoutes.Location.PostLocation,
            new UpdateLocationRequest(Latitude: latitude, Longitude: longitude));
        response.Success.ShouldBeTrue();
    }
}