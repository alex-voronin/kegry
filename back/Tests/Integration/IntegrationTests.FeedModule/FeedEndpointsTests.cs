using FeedModule.API.Dto;
using IntegrationTests.Shared;
using LocationModule.API.Dto;
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
        // 1. Sign in
        var email = "feedtest@example.com";
        var password = "Password123!";
        var accessToken = await _factory.AuthManager.CreateAndLoginUserAsync(email, password);
        
        // 2. Use access token in following requests 
        accessToken.ShouldNotBeNullOrEmpty();
        _factory.ApiClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        
        // 3. Create the profile
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

        // 4. Submit our location as it necessary to load a feed
        await _factory.ApiClient.PostAsync<UpdateLocationRequest, ApiResult.EmptyResult>("/api/v1/location",
            new UpdateLocationRequest(Latitude: 0.0, Longitude: 0.0));

        // 5. Get Feed
        var feedResponse = await _factory.ApiClient.GetAsync<ApiResult<GetFeedResponse>>("/api/v1/feed");

        feedResponse.Success.ShouldBeTrue();
        feedResponse.Result.ShouldNotBeNull();
        feedResponse.Result.Feed.ShouldNotBeNull();
    }
}