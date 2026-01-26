using System.Net.Http.Json;
using FeedModule.API.Dto;
using FeedModule.Domain.Services;
using FeedModule.Domain.ValueObjects;
using IntegrationTests.Shared;
using IntegrationTests.Shared.HttpClients;
using SharedCore.Api.Dto;
using Shouldly;

namespace IntegrationTests.FeedModule;

[Collection("IntegrationTests")]
public class FeedEndpointsTests : IntegrationTestBase, IAsyncLifetime
{
    public FeedEndpointsTests(IntegrationWebApplicationFactory factory) : base(factory)
    {
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public async Task DisposeAsync() => await ContainerManager.ResetDatabaseAsync();

    [Fact]
    public async Task GivenOneUser_WhenOpensFeed_ThenReturnsNoUsers()
    {
        var email = "feedtest@example.com";
        var password = "Password123!";
        var authResponse = await Factory.AuthManager.CreateAndLoginUserAsync(email, password);
        Factory.ApiClient.SetAuth(authResponse);

        await Factory.AuthManager.CreateDefaultProfileAndSubmitLocation();

        var feedResponse = await Factory.ApiClient.GetAsync<ApiResult<GetFeedResponse>>(ApiRoutes.Feed.GetFeed);
        feedResponse.Success.ShouldBeTrue();
        feedResponse.Result.ShouldNotBeNull();
        feedResponse.Result.Feed.ShouldNotBeNull();
        feedResponse.Result.Feed.Length.ShouldBe(0);
    }

    [Fact]
    public async Task GivenTwoUsers_WhenUserRatesAnotherUser_ThenTheUserSeesRateOnFeed()
    {
        // 1. Sign in User A (Rater)
        var authResponseA = await Factory.AuthManager.CreateAndLoginUserAsync("rater@example.com", "Password123!");
        Factory.ApiClient.SetAuth(authResponseA);
        await Factory.AuthManager.CreateDefaultProfileAndSubmitLocation();

        // 2. Create User B (Rated)
        var authResponseB = await Factory.AuthManager.CreateAndLoginUserAsync("ratee@example.com", "Password123!");
        var userBId = authResponseB.UserId;
        Factory.ApiClient.SetAuth(authResponseB);
        await Factory.AuthManager.CreateDefaultProfileAndSubmitLocation();

        // 3. User A rates User B
        Factory.ApiClient.SetAuth(authResponseA);
        var rating = 4.5m;
        var ratingRequest = new UserRatingDto(rating);
        var response = await Factory.ApiClient.PostAsync<UserRatingDto, ApiResult.EmptyResult>(
            ApiRoutes.Feed.RateUser(userBId), ratingRequest);

        response.Success.ShouldBeTrue(
            $"Rating failed with errors: {string.Join(", ", response.Errors.Select(e => e.Error))}");

        // 4. Get Feed and check rating
        var feedResponse = await Factory.ApiClient.GetAsync<ApiResult<GetFeedResponse>>(ApiRoutes.Feed.GetFeed);
        feedResponse.Result.Feed.First().Rating.ShouldBe(rating);
    }

    [Fact]
    public async Task GivenTwoUsers_WhenUserSkipsAnotherUser_ThenAnotherUserDisappearsForSkipCutOffTime()
    {
        // 1. Create two users and submit locations
        var authA = await Factory.AuthManager.CreateAndLoginUserAsync("blocker@example.com", "Password123!");
        Factory.ApiClient.SetAuth(authA);
        await Factory.AuthManager.CreateDefaultProfileAndSubmitLocation();

        var authB = await Factory.AuthManager.CreateAndLoginUserAsync("blockee@example.com", "Password123!");
        var userBId = authB.UserId;
        Factory.ApiClient.SetAuth(authB);
        await Factory.AuthManager.CreateDefaultProfileAndSubmitLocation();

        // 2. User A sees User B in feed
        Factory.ApiClient.SetAuth(authA);
        var feedBefore = await Factory.ApiClient.GetAsync<ApiResult<GetFeedResponse>>(ApiRoutes.Feed.GetFeed);
        feedBefore.Result.Feed.Any(f => f.Id == Guid.Parse(userBId)).ShouldBeTrue();

        // 3. User A blocks User B
        var response = await Factory.ApiClient.PostAsync<object?, ApiResult.EmptyResult>(
            ApiRoutes.Feed.SkipUser(userBId), null);
        response.Success.ShouldBeTrue();

        // 4. User A no longer sees User B in feed
        var feedAfterSkip = await Factory.ApiClient.GetAsync<ApiResult<GetFeedResponse>>(ApiRoutes.Feed.GetFeed);
        feedAfterSkip.Result.Feed.Any(f => f.Id == Guid.Parse(userBId)).ShouldBeFalse();

        //5. User A can see User B in feed after time passed
        Factory.FakeTimeProvider.Advance(TimeSpan.FromDays(FeedParameters.SkippedUserCutOffInDays + 1));
        var feedAfterDelay = await Factory.ApiClient.GetAsync<ApiResult<GetFeedResponse>>(ApiRoutes.Feed.GetFeed);
        feedAfterDelay.Result.Feed.Any(f => f.Id == Guid.Parse(userBId)).ShouldBeTrue();
    }

    [Fact]
    public async Task GivenTwoUsers_WhenUserBlocksAnotherUser_ThenAnotherUserDisappearsFromFeed()
    {
        // 1. Create two users and submit locations
        var authA = await Factory.AuthManager.CreateAndLoginUserAsync("blocker@example.com", "Password123!");
        Factory.ApiClient.SetAuth(authA);
        await Factory.AuthManager.CreateDefaultProfileAndSubmitLocation();

        var authB = await Factory.AuthManager.CreateAndLoginUserAsync("blockee@example.com", "Password123!");
        var userBId = authB.UserId;
        Factory.ApiClient.SetAuth(authB);
        await Factory.AuthManager.CreateDefaultProfileAndSubmitLocation();

        // 2. User A sees User B in feed
        Factory.ApiClient.SetAuth(authA);
        var feedBefore = await Factory.ApiClient.GetAsync<ApiResult<GetFeedResponse>>(ApiRoutes.Feed.GetFeed);
        feedBefore.Result.Feed.Any(f => f.Id == Guid.Parse(userBId)).ShouldBeTrue();

        // 3. User A blocks User B
        var response = await Factory.ApiClient.PostAsync<object?, ApiResult.EmptyResult>(
            ApiRoutes.Feed.BlockUser(userBId), null);
        response.Success.ShouldBeTrue();

        // 4. User A no longer sees User B in feed
        Factory.FakeTimeProvider.Advance(TimeSpan.FromDays(365));
        var feedAfter = await Factory.ApiClient.GetAsync<ApiResult<GetFeedResponse>>(ApiRoutes.Feed.GetFeed);
        feedAfter.Result.Feed.Any(f => f.Id == Guid.Parse(userBId)).ShouldBeFalse();
    }

    [Fact]
    public async Task GivenTwoUsers_WhenUserIgnoresAnotherUser_ThenThisUserGoesToEndOfTheFeed()
    {
        // 1. Create two users and submit locations
        var authA = await Factory.AuthManager.CreateAndLoginUserAsync("user_a@example.com", "Password123!");
        Factory.ApiClient.SetAuth(authA);
        await Factory.AuthManager.CreateDefaultProfileAndSubmitLocation();

        var authB = await Factory.AuthManager.CreateAndLoginUserAsync("user_b@example.com", "Password123!");
        var userBId = authB.UserId;
        Factory.ApiClient.SetAuth(authB);
        await Factory.AuthManager.CreateDefaultProfileAndSubmitLocation();

        var authC = await Factory.AuthManager.CreateAndLoginUserAsync("user_c@example.com", "Password123!");
        Factory.ApiClient.SetAuth(authC);
        await Factory.AuthManager.CreateProfile();
        await Factory.AuthManager.SubmitLocation(0.01, 0.01);

        // 2. User A sees User B in feed
        Factory.ApiClient.SetAuth(authA);

        var feedBefore = await Factory.ApiClient.GetAsync<ApiResult<GetFeedResponse>>(ApiRoutes.Feed.GetFeed);
        var firstFeedId = feedBefore.Result.Feed.First().Id;
        firstFeedId.ShouldBe(Guid.Parse(userBId));

        // 3. User A ignore User B
        var response = await Factory.ApiClient.PostAsync<object?, ApiResult.EmptyResult>(
            ApiRoutes.Feed.IgnoreUser(userBId), null);
        response.Success.ShouldBeTrue();

        // 4. User B goes to the end of the feed
        var feedAfterIgnore = await Factory.ApiClient.GetAsync<ApiResult<GetFeedResponse>>(ApiRoutes.Feed.GetFeed);
        feedAfterIgnore.Result.Feed.Last().Id.ShouldBe(Guid.Parse(userBId));

        // 5. After delay user B returns on top of the feed
        Factory.FakeTimeProvider.Advance(TimeSpan.FromHours(FeedParameters.IgnoredUserRatingDowngradeInHours + 1));
        var feedAfterDelay = await Factory.ApiClient.GetAsync<ApiResult<GetFeedResponse>>(ApiRoutes.Feed.GetFeed);
        feedAfterDelay.Result.Feed.First().Id.ShouldBe(Guid.Parse(userBId));
    }

    [Fact]
    public async Task GivenTwoUsers_WhenUserInvitesAnotherUser_ThenReturnsSuccess()
    {
        // 1. Create two users and submit locations
        var authA = await Factory.AuthManager.CreateAndLoginUserAsync("inviter@example.com", "Password123!");
        Factory.ApiClient.SetAuth(authA);
        await Factory.AuthManager.CreateDefaultProfileAndSubmitLocation();

        var authB = await Factory.AuthManager.CreateAndLoginUserAsync("invitee@example.com", "Password123!");
        var userBId = authB.UserId;
        Factory.ApiClient.SetAuth(authB);
        await Factory.AuthManager.CreateDefaultProfileAndSubmitLocation();

        // 2. User A invites User B
        Factory.ApiClient.SetAuth(authA);
        var response = await Factory.ApiClient.PostAsync<object?, ApiResult.EmptyResult>(
            ApiRoutes.Feed.InviteUser(userBId), null);

        response.Success.ShouldBeTrue();
    }

    [Fact]
    public async Task GivenThreeUsers_WhenTwoUsersInviteSameUserSimultaneously_ThenOnlyOneMeetingEstablished()
    {
        // 1. Create three users A, B, and C
        var authA = await Factory.AuthManager.CreateAndLoginUserAsync("usera@example.com", "Password123!");
        Factory.ApiClient.SetAuth(authA);
        await Factory.AuthManager.CreateDefaultProfileAndSubmitLocation();

        var authB = await Factory.AuthManager.CreateAndLoginUserAsync("userb@example.com", "Password123!");
        Factory.ApiClient.SetAuth(authB);
        await Factory.AuthManager.CreateDefaultProfileAndSubmitLocation();

        var authC = await Factory.AuthManager.CreateAndLoginUserAsync("userc@example.com", "Password123!");
        var userCId = authC.UserId;
        Factory.ApiClient.SetAuth(authC);
        await Factory.AuthManager.CreateDefaultProfileAndSubmitLocation();

        // 2. A and B both invite C
        Factory.ApiClient.SetAuth(authC);
        await Factory.ApiClient.PostAsync<object?, ApiResult.EmptyResult>(ApiRoutes.Feed.InviteUser(authA.UserId),
            null);
        await Factory.ApiClient.PostAsync<object?, ApiResult.EmptyResult>(ApiRoutes.Feed.InviteUser(authB.UserId),
            null);

        // 3. A and B invite C simultaneously
        using var httpClientA = Factory.CreateClient();
        httpClientA.SetAuth(authA);
        var taskA = httpClientA.PostAsync(ApiRoutes.Feed.InviteUser(userCId), null);

        using var httpClientB = Factory.CreateClient();
        httpClientB.SetAuth(authB);
        var taskB = httpClientA.PostAsync(ApiRoutes.Feed.InviteUser(userCId), null);
        var results = await Task.WhenAll(taskA, taskB);

        // 4. Check A status 
        var inviteA = await results[0].Content.ReadFromJsonAsync<ApiResult.EmptyResult>();
        inviteA!.Success.ShouldBeTrue();
        Factory.ApiClient.SetAuth(authA);
        var statusA = await Factory.ApiClient.GetAsync<ApiResult<GetFeedStatusResponse>>(ApiRoutes.Feed.GetStatus);
        statusA.Success.ShouldBeTrue();

        // 5. Check B status 
        var inviteB = await results[1].Content.ReadFromJsonAsync<ApiResult.EmptyResult>();
        inviteB!.Success.ShouldBeTrue();
        Factory.ApiClient.SetAuth(authB);
        var statusB = await Factory.ApiClient.GetAsync<ApiResult<GetFeedStatusResponse>>(ApiRoutes.Feed.GetStatus);
        statusB.Success.ShouldBeTrue();

        // 6. A and B in meetings status must be different. One should have established the meeting, the other not.
        statusA.Result.IsInMeeting.ShouldNotBe(statusB.Result.IsInMeeting);

        // 7. Verify that C is in the meeting.
        Factory.ApiClient.SetAuth(authC);
        var statusC = await Factory.ApiClient.GetAsync<ApiResult<GetFeedStatusResponse>>(ApiRoutes.Feed.GetStatus);
        statusC.Result.IsInMeeting.ShouldBeTrue();
    }

    [Fact]
    public async Task WhenUserSkipsMultipleUsers_ThenFeedIsRefilledWithNewUsers()
    {
        const int usersToSkipCount = 5;

        // 1. Create main user
        var authMain = await Factory.AuthManager.CreateAndLoginUserAsync("main@example.com", "Password123!");
        Factory.ApiClient.SetAuth(authMain);
        await Factory.AuthManager.CreateDefaultProfileAndSubmitLocation();

        // 2. Create FeedParameters.FeedLength + 5 candidate users
        const int totalCandidates = FeedParameters.FeedLength + usersToSkipCount;
        for (var i = 0; i < totalCandidates; i++)
        {
            var authCandidate =
                await Factory.AuthManager.CreateAndLoginUserAsync($"candidate{i}@example.com", "Password123!");
            Factory.ApiClient.SetAuth(authCandidate);
            await Factory.AuthManager.CreateDefaultProfileAndSubmitLocation();
        }

        // 3. Get initial feed for main user
        Factory.ApiClient.SetAuth(authMain);
        var initialFeedResponse = await Factory.ApiClient.GetAsync<ApiResult<GetFeedResponse>>(ApiRoutes.Feed.GetFeed);
        initialFeedResponse.Success.ShouldBeTrue();
        var initialFeed = initialFeedResponse.Result.Feed;
        initialFeed.Length.ShouldBe(FeedParameters.FeedLength);

        // 4. Skip first users from the initial feed
        var userIdsToSkip = initialFeed.Select(p => p.Id).Take(usersToSkipCount).ToHashSet();
        foreach (var userId in userIdsToSkip)
        {
            var skipResponse = await Factory.ApiClient.PostAsync<object?, ApiResult.EmptyResult>(
                ApiRoutes.Feed.SkipUser(userId.ToString()), null);
            skipResponse.Success.ShouldBeTrue();
        }

        // 5. Get feed again
        var feedAfterSkipResponse =
            await Factory.ApiClient.GetAsync<ApiResult<GetFeedResponse>>(ApiRoutes.Feed.GetFeed);
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

    [Fact]
    public async Task GivenBlockedUsers_WhenGetBlockedUsers_ThenReturnsList()
    {
        // 1. Create two users and submit locations
        var authA = await Factory.AuthManager.CreateAndLoginUserAsync("blocker2@example.com", "Password123!");
        Factory.ApiClient.SetAuth(authA);
        await Factory.AuthManager.CreateDefaultProfileAndSubmitLocation();

        var authB = await Factory.AuthManager.CreateAndLoginUserAsync("blockee2@example.com", "Password123!");
        var userBId = authB.UserId;
        Factory.ApiClient.SetAuth(authB);
        await Factory.AuthManager.CreateDefaultProfileAndSubmitLocation();

        // 2. User A blocks User B
        Factory.ApiClient.SetAuth(authA);
        var response = await Factory.ApiClient.PostAsync<object?, ApiResult.EmptyResult>(
            ApiRoutes.Feed.BlockUser(userBId), null);
        response.Success.ShouldBeTrue();

        // 3. User A retrieves blocked users
        var blockedUsersResponse = await Factory.ApiClient.GetAsync<ApiResult<PaginatedResponse<BlockedUserDto>>>(ApiRoutes.Feed.GetBlockedUsers);
        blockedUsersResponse.Success.ShouldBeTrue();

        // 4. Assert response contains User B
        blockedUsersResponse.Result.Data.ShouldNotBeEmpty();
        var blockedUser = blockedUsersResponse.Result.Data.First();
        blockedUser.Id.ShouldBe(Guid.Parse(userBId));
        blockedUser.FirstName.ShouldNotBeNullOrEmpty();
        blockedUser.LastName.ShouldNotBeNullOrEmpty();
        blockedUser.AvatarUrl.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task GivenUserWithoutSavedSettings_WhenGetDisplaySettings_ThenDefaultsReturned()
    {
        // 1. Create user
        var auth = await Factory.AuthManager.CreateAndLoginUserAsync("default_settings@example.com", "Password123!");
        Factory.ApiClient.SetAuth(auth);

        // 2. Get settings
        var response = await Factory.ApiClient.GetAsync<ApiResult<GetSettingsResponse>>(ApiRoutes.Feed.DisplaySettings);

        // 3. Verify defaults
        response.Success.ShouldBeTrue();
        response.Result.Settings.DistanceUnits.ShouldBe(DistanceUnits.Kilometers);
    }

    [Fact]
    public async Task GivenUser_WhenUpdateDisplaySettings_ThenSettingsAreUpdated()
    {
        // 1. Create user
        var auth = await Factory.AuthManager.CreateAndLoginUserAsync("update_settings@example.com", "Password123!");
        Factory.ApiClient.SetAuth(auth);

        // 2. Update settings
        var newSettings = new UpdateSettingsRequest(DistanceUnits.Miles);
        var updateResponse = await Factory.ApiClient.PutAsync<UpdateSettingsRequest, ApiResult.EmptyResult>(
            ApiRoutes.Feed.DisplaySettings, newSettings);
        updateResponse.Success.ShouldBeTrue();

        // 3. Verify update
        var getResponse = await Factory.ApiClient.GetAsync<ApiResult<GetSettingsResponse>>(ApiRoutes.Feed.DisplaySettings);
        getResponse.Success.ShouldBeTrue();
        getResponse.Result.Settings.DistanceUnits.ShouldBe(DistanceUnits.Miles);
    }
}