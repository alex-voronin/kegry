using FeedModule.API.Dto;
using IntegrationTests.Shared;
using SharedCore.Api.Dto;
using Shouldly;

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
    public async Task GivenOneUser_WhenTheyOpenFeed_ThenThereNoUsers()
    {
        var email = "feedtest@example.com";
        var password = "Password123!";
        var authResponse = await _factory.AuthManager.CreateAndLoginUserAsync(email, password);
        _factory.ApiClient.SetAuth(authResponse);
        
        await _factory.AuthManager.CreateDefaultProfileAndSubmitLocation();
        
        var feedResponse = await _factory.ApiClient.GetAsync<ApiResult<GetFeedResponse>>("/api/v1/feed");
        feedResponse.Success.ShouldBeTrue();
        feedResponse.Result.ShouldNotBeNull();
        feedResponse.Result.Feed.ShouldNotBeNull();
        feedResponse.Result.Feed.Length.ShouldBe(0);
    }

    [Fact]
    public async Task GivenTwoUsers_WhenUserRatesAnotherUser_ThenTheUserSeesRateOnFeed()
    {
        // 1. Sign in User A (Rater)
        var authResponseA = await _factory.AuthManager.CreateAndLoginUserAsync("rater@example.com", "Password123!");
        _factory.ApiClient.SetAuth(authResponseA);
        await _factory.AuthManager.CreateDefaultProfileAndSubmitLocation();
        
        // 2. Create User B (Rated)
        var authResponseB = await _factory.AuthManager.CreateAndLoginUserAsync("ratee@example.com", "Password123!");
        var userBId = authResponseB.UserId;
        _factory.ApiClient.SetAuth(authResponseB);
        await _factory.AuthManager.CreateDefaultProfileAndSubmitLocation();

        // 3. User A rates User B
        _factory.ApiClient.SetAuth(authResponseA);
        var rating = 4.5m;
        var ratingRequest = new UserRatingDto(rating);
        var response = await _factory.ApiClient.PostAsync<UserRatingDto, ApiResult.EmptyResult>(
            $"/api/v1/feed/{userBId}/rating", ratingRequest);
        
        response.Success.ShouldBeTrue(
            $"Rating failed with errors: {string.Join(", ", response.Errors.Select(e => e.Error))}");
        
        // 4. Get Feed and check rating
        var feedResponse = await _factory.ApiClient.GetAsync<ApiResult<GetFeedResponse>>("/api/v1/feed");
        feedResponse.Result.Feed.First().Rating.ShouldBe(rating);
    }
}