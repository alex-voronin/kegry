using System.Net.Http.Json;
using FeedModule.API.Dto;
using FeedModule.Domain.Services;
using IntegrationTests.Shared;
using IntegrationTests.Shared.HttpClients;
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
    public async Task GivenOneUser_WhenOpensFeed_ThenReturnsNoUsers()
    {
        var email = "feedtest@example.com";
        var password = "Password123!";
        var authResponse = await _factory.AuthManager.CreateAndLoginUserAsync(email, password);
        _factory.ApiClient.SetAuth(authResponse);

        await _factory.AuthManager.CreateDefaultProfileAndSubmitLocation();

        var feedResponse = await _factory.ApiClient.GetAsync<ApiResult<GetFeedResponse>>(ApiRoutes.Feed.GetFeed);
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
            ApiRoutes.Feed.RateUser(userBId), ratingRequest);

        response.Success.ShouldBeTrue(
            $"Rating failed with errors: {string.Join(", ", response.Errors.Select(e => e.Error))}");

        // 4. Get Feed and check rating
        var feedResponse = await _factory.ApiClient.GetAsync<ApiResult<GetFeedResponse>>(ApiRoutes.Feed.GetFeed);
        feedResponse.Result.Feed.First().Rating.ShouldBe(rating);
    }

    [Fact]
    public async Task GivenTwoUsers_WhenUserSkipsAnotherUser_ThenAnotherUserDisappearsForSkipCutOffTime()
    {
        _factory.FakeTimeProvider.SetUtcNow(DateTime.UtcNow);

        // 1. Create two users and submit locations
        var authA = await _factory.AuthManager.CreateAndLoginUserAsync("blocker@example.com", "Password123!");
        _factory.ApiClient.SetAuth(authA);
        await _factory.AuthManager.CreateDefaultProfileAndSubmitLocation();

        var authB = await _factory.AuthManager.CreateAndLoginUserAsync("blockee@example.com", "Password123!");
        var userBId = authB.UserId;
        _factory.ApiClient.SetAuth(authB);
        await _factory.AuthManager.CreateDefaultProfileAndSubmitLocation();

        // 2. User A sees User B in feed
        _factory.ApiClient.SetAuth(authA);
        var feedBefore = await _factory.ApiClient.GetAsync<ApiResult<GetFeedResponse>>(ApiRoutes.Feed.GetFeed);
        feedBefore.Result.Feed.Any(f => f.Id == Guid.Parse(userBId)).ShouldBeTrue();

        // 3. User A blocks User B
        var response = await _factory.ApiClient.PostAsync<object?, ApiResult.EmptyResult>(
            ApiRoutes.Feed.SkipUser(userBId), null);
        response.Success.ShouldBeTrue();

        // 4. User A no longer sees User B in feed
        var feedAfterSkip = await _factory.ApiClient.GetAsync<ApiResult<GetFeedResponse>>(ApiRoutes.Feed.GetFeed);
        feedAfterSkip.Result.Feed.Any(f => f.Id == Guid.Parse(userBId)).ShouldBeFalse();

        //5. User A can see User B in feed after time passed
        _factory.FakeTimeProvider.Advance(TimeSpan.FromDays(FeedParameters.SkippedUserCutOffInDays + 1));
        var feedAfterDelay = await _factory.ApiClient.GetAsync<ApiResult<GetFeedResponse>>(ApiRoutes.Feed.GetFeed);
        feedAfterDelay.Result.Feed.Any(f => f.Id == Guid.Parse(userBId)).ShouldBeTrue();
    }

    [Fact]
    public async Task GivenTwoUsers_WhenUserBlocksAnotherUser_ThenAnotherUserDisappearsFromFeed()
    {
        // 1. Create two users and submit locations
        var authA = await _factory.AuthManager.CreateAndLoginUserAsync("blocker@example.com", "Password123!");
        _factory.ApiClient.SetAuth(authA);
        await _factory.AuthManager.CreateDefaultProfileAndSubmitLocation();

        var authB = await _factory.AuthManager.CreateAndLoginUserAsync("blockee@example.com", "Password123!");
        var userBId = authB.UserId;
        _factory.ApiClient.SetAuth(authB);
        await _factory.AuthManager.CreateDefaultProfileAndSubmitLocation();

        // 2. User A sees User B in feed
        _factory.ApiClient.SetAuth(authA);
        var feedBefore = await _factory.ApiClient.GetAsync<ApiResult<GetFeedResponse>>(ApiRoutes.Feed.GetFeed);
        feedBefore.Result.Feed.Any(f => f.Id == Guid.Parse(userBId)).ShouldBeTrue();

        // 3. User A blocks User B
        var response = await _factory.ApiClient.PostAsync<object?, ApiResult.EmptyResult>(
            ApiRoutes.Feed.BlockUser(userBId), null);
        response.Success.ShouldBeTrue();

        // 4. User A no longer sees User B in feed
        _factory.FakeTimeProvider.Advance(TimeSpan.FromDays(365));
        var feedAfter = await _factory.ApiClient.GetAsync<ApiResult<GetFeedResponse>>(ApiRoutes.Feed.GetFeed);
        feedAfter.Result.Feed.Any(f => f.Id == Guid.Parse(userBId)).ShouldBeFalse();
    }

    [Fact]
    public async Task GivenTwoUsers_WhenUserInvitesAnotherUser_ThenReturnsSuccess()
    {
        // 1. Create two users and submit locations
        var authA = await _factory.AuthManager.CreateAndLoginUserAsync("inviter@example.com", "Password123!");
        _factory.ApiClient.SetAuth(authA);
        await _factory.AuthManager.CreateDefaultProfileAndSubmitLocation();

        var authB = await _factory.AuthManager.CreateAndLoginUserAsync("invitee@example.com", "Password123!");
        var userBId = authB.UserId;
        _factory.ApiClient.SetAuth(authB);
        await _factory.AuthManager.CreateDefaultProfileAndSubmitLocation();

        // 2. User A invites User B
        _factory.ApiClient.SetAuth(authA);
        var response = await _factory.ApiClient.PostAsync<object?, ApiResult.EmptyResult>(
            ApiRoutes.Feed.InviteUser(userBId), null);

        response.Success.ShouldBeTrue();
    }

    [Fact]
    public async Task GivenThreeUsers_WhenTwoUsersInviteSameUserSimultaneously_ThenOnlyOneMeetingEstablished()
    {
        // 1. Create three users A, B, and C
        var authA = await _factory.AuthManager.CreateAndLoginUserAsync("usera@example.com", "Password123!");
        _factory.ApiClient.SetAuth(authA);
        await _factory.AuthManager.CreateDefaultProfileAndSubmitLocation();

        var authB = await _factory.AuthManager.CreateAndLoginUserAsync("userb@example.com", "Password123!");
        _factory.ApiClient.SetAuth(authB);
        await _factory.AuthManager.CreateDefaultProfileAndSubmitLocation();

        var authC = await _factory.AuthManager.CreateAndLoginUserAsync("userc@example.com", "Password123!");
        var userCId = authC.UserId;
        _factory.ApiClient.SetAuth(authC);
        await _factory.AuthManager.CreateDefaultProfileAndSubmitLocation();

        // 2. A and B both invite C
        _factory.ApiClient.SetAuth(authC);
        await _factory.ApiClient.PostAsync<object?, ApiResult.EmptyResult>(ApiRoutes.Feed.InviteUser(authA.UserId),
            null);
        await _factory.ApiClient.PostAsync<object?, ApiResult.EmptyResult>(ApiRoutes.Feed.InviteUser(authB.UserId),
            null);

        // 3. A and B invite C simultaneously
        using var httpClientA = _factory.CreateClient();
        httpClientA.SetAuth(authA);
        var taskA = httpClientA.PostAsync(ApiRoutes.Feed.InviteUser(userCId), null);

        using var httpClientB = _factory.CreateClient();
        httpClientB.SetAuth(authB);
        var taskB = httpClientA.PostAsync(ApiRoutes.Feed.InviteUser(userCId), null);
        var results = await Task.WhenAll(taskA, taskB);

        // 4. Check A status 
        var inviteA = await results[0].Content.ReadFromJsonAsync<ApiResult.EmptyResult>();
        inviteA!.Success.ShouldBeTrue();
        _factory.ApiClient.SetAuth(authA);
        var statusA = await _factory.ApiClient.GetAsync<ApiResult<GetFeedStatusResponse>>(ApiRoutes.Feed.GetStatus);
        statusA.Success.ShouldBeTrue();

        // 5. Check B status 
        var inviteB = await results[1].Content.ReadFromJsonAsync<ApiResult.EmptyResult>();
        inviteB!.Success.ShouldBeTrue();
        _factory.ApiClient.SetAuth(authB);
        var statusB = await _factory.ApiClient.GetAsync<ApiResult<GetFeedStatusResponse>>(ApiRoutes.Feed.GetStatus);
        statusB.Success.ShouldBeTrue();

        // 6. A and B in meetings status must be different. One should have established the meeting, the other not.
        statusA.Result.IsInMeeting.ShouldNotBe(statusB.Result.IsInMeeting);

        // 7. Verify that C is in the meeting.
        _factory.ApiClient.SetAuth(authC);
        var statusC = await _factory.ApiClient.GetAsync<ApiResult<GetFeedStatusResponse>>(ApiRoutes.Feed.GetStatus);
        statusC.Result.IsInMeeting.ShouldBeTrue();
    }
    [Fact]
    public async Task WhenUserSkipsMultipleUsers_ThenFeedIsRefilledWithNewUsers()
    {
        const int usersToSkipCount = 5;

        // 1. Create main user
        var authMain = await _factory.AuthManager.CreateAndLoginUserAsync("main@example.com", "Password123!");
        _factory.ApiClient.SetAuth(authMain);
        await _factory.AuthManager.CreateDefaultProfileAndSubmitLocation();

        // 2. Create FeedParameters.FeedLength + 5 candidate users
        const int totalCandidates = FeedParameters.FeedLength + usersToSkipCount;
        for (var i = 0; i < totalCandidates; i++)
        {
            var authCandidate = await _factory.AuthManager.CreateAndLoginUserAsync($"candidate{i}@example.com", "Password123!");
            _factory.ApiClient.SetAuth(authCandidate);
            await _factory.AuthManager.CreateDefaultProfileAndSubmitLocation();
        }

        // 3. Get initial feed for main user
        _factory.ApiClient.SetAuth(authMain);
        var initialFeedResponse = await _factory.ApiClient.GetAsync<ApiResult<GetFeedResponse>>(ApiRoutes.Feed.GetFeed);
        initialFeedResponse.Success.ShouldBeTrue();
        var initialFeed = initialFeedResponse.Result.Feed;
        initialFeed.Length.ShouldBe(FeedParameters.FeedLength);

        // 4. Skip first users from the initial feed
        var userIdsToSkip = initialFeed.Select(p => p.Id).Take(usersToSkipCount).ToHashSet();
        foreach (var userId in userIdsToSkip)
        {
            var skipResponse = await _factory.ApiClient.PostAsync<object?, ApiResult.EmptyResult>(
                ApiRoutes.Feed.SkipUser(userId.ToString()), null);
            skipResponse.Success.ShouldBeTrue();
        }

        // 5. Get feed again
        var feedAfterSkipResponse = await _factory.ApiClient.GetAsync<ApiResult<GetFeedResponse>>(ApiRoutes.Feed.GetFeed);
        feedAfterSkipResponse.Success.ShouldBeTrue();

        // 6. Validate actual feed
        var actualFeed = feedAfterSkipResponse.Result.Feed;
        actualFeed.Length.ShouldBe(FeedParameters.FeedLength);

        var actualUserIds = actualFeed.Select(p => p.Id).ToHashSet();
        var hasSkippedUsers = actualUserIds.Intersect(userIdsToSkip).Any();
        hasSkippedUsers.ShouldBeFalse();

        var initialUserIds = initialFeed.Select(f => f.Id).ToHashSet();
        var newUserCount = actualUserIds.Except(initialUserIds).Count();
        newUserCount.ShouldBe(usersToSkipCount);
    }
}