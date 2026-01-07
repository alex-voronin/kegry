using FeedModule.API.Dto;
using IntegrationTests.Shared.Setup;
using LocationModule.API.Dto;
using Microsoft.Extensions.DependencyInjection;
using SecurityModule.API.Dto;
using SecurityModule.Domain.Aggregates.User;
using SecurityModule.Infrastructure.Persistence;
using SharedCore.Api.Dto;
using SharedCore.ValueObjects;
using Shouldly;
using UserProfileModule.API.Dto;

namespace IntegrationTests.FeedModule;

[Collection("IntegrationTests")]
public class FeedEndpointsTests : IAsyncLifetime
{
    private readonly IntegrationWebApplicationFactory _factory;

    public FeedEndpointsTests(IntegrationWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public async Task DisposeAsync() => await _factory.ResetDatabaseAsync();

    [Fact]
    public async Task GetFeed_ShouldReturnFeed_WhenUserAuthenticated()
    {
        // 1. Sign up
        var email = "feedtest@example.com";
        var password = "Password123!";
        var request = new SignInRequest(email, password);

        var signinResponse = await _factory.ApiClient.PostAsync<SignInRequest, ApiResult<AuthResponse>>(
            "/api/v1/auth/signin", request);

        signinResponse.Success.ShouldBeTrue();
        var userId = Guid.Parse(signinResponse.Result.UserId);

        // 2. Manually verify user in DB
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<SecurityContext>();
            var user = await context.Users.FindAsync(userId);
            user.ShouldNotBeNull();
            user.RequiredAction = RequiredAction.None;
            user.EmailConfirmed = true;
            await context.SaveChangesAsync();
        }
        
        // 3. Sign in again to get Valid Token
        var loginResponse = await _factory.ApiClient.PostAsync<SignInRequest, ApiResult<AuthResponse>>(
            "/api/v1/auth/signin", request);

        loginResponse.Success.ShouldBeTrue(
            $"Login failed with errors: {string.Join(", ", loginResponse.Errors.Select(e => e.Error))}");

        // 4. Use access token in following requests 
        var accessToken = loginResponse.Result.AccessToken;
        accessToken.ShouldNotBeNullOrEmpty();
        _factory.ApiClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        
        // 5. Create the profile
        await _factory.ApiClient.PostAsync<CreateUserProfileRequest, ApiResult.EmptyResult>("/api/v1/user-profile",
            new CreateUserProfileRequest(
                FirstName:"FirstName",
                LastName:"LastName",
                Bio:"Bio",
                AvatarImageUrl:"http://my-image-url/avatar.jpg",
                CoverImageUrl:"http://my-image-url/cover-image-url.jpg",
                Birthday: new DateOnly(1980, 1, 1),
                Gender: Gender.Male,
                HobbyIds:[]
            )
        );

        // 6. Submit our location as it necessary to load a feed
        await _factory.ApiClient.PostAsync<UpdateLocationRequest, ApiResult.EmptyResult>("/api/v1/location",
            new UpdateLocationRequest(Latitude: 0.0, Longitude: 0.0));

        // 7. Get Feed
        var feedResponse = await _factory.ApiClient.GetAsync<ApiResult<GetFeedResponse>>("/api/v1/feed");

        feedResponse.Success.ShouldBeTrue();
        feedResponse.Result.ShouldNotBeNull();
        feedResponse.Result.Feed.ShouldNotBeNull();
    }
}