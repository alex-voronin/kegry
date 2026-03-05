using System.Net.Http.Json;
using FeedModule.API.Dto;
using FeedModule.Domain.Aggregates;
using FeedModule.Domain.Repositories;
using FeedModule.Domain.Services;
using FeedModule.Domain.ValueObjects;
using FeedModule.Infrastructure.Persistence;
using IntegrationTests.Shared;
using IntegrationTests.Shared.HttpClients;
using Microsoft.Extensions.DependencyInjection;
using NotificationsModule.API.Dto;
using SharedCore.Api.Dto;
using SharedCore.Exceptions;
using Shouldly;
using UserProfileModule.API.Dto;
using Wolverine.Tracking;

namespace IntegrationTests.FeedModule;

[Collection("IntegrationTests")]
public class FeedEndpointsTests : IntegrationTestBase, IAsyncLifetime
{
    public FeedEndpointsTests(IntegrationWebApplicationFactory factory) : base(factory)
    {
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public async Task DisposeAsync() => await ContainerManager.ResetDatabaseAsync();

    private DateTimeOffset ComputeExpiresAtForNowOption()
    {
        const int expiresNowDurationHours = 4;
        return Factory.FakeTimeProvider.GetUtcNow().AddHours(expiresNowDurationHours);
    }

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
    public async Task GivenTwoUsers_WhenUserIsNonPublic_ThenTheyDisappearFromFeed()
    {
        // 1. Create two users and submit locations
        var authA = await Factory.AuthManager.CreateAndLoginUserAsync("user_a@example.com", "Password123!");
        Factory.ApiClient.SetAuth(authA);
        await Factory.AuthManager.CreateDefaultProfileAndSubmitLocation();

        var authB = await Factory.AuthManager.CreateAndLoginUserAsync("user_b@example.com", "Password123!");
        var userBId = authB.UserId;
        Factory.ApiClient.SetAuth(authB);
        await Factory.AuthManager.CreateDefaultProfileAndSubmitLocation();

        // 2. User A sees User B in feed
        Factory.ApiClient.SetAuth(authA);
        var feedBefore = await Factory.ApiClient.GetAsync<ApiResult<GetFeedResponse>>(ApiRoutes.Feed.GetFeed);
        var firstFeedId = feedBefore.Result.Feed.First().Id;
        firstFeedId.ShouldBe(Guid.Parse(userBId));

        // 3. User B makes the account non-public
        Factory.ApiClient.SetAuth(authB);
        var request = new UpdatePrivacyRequest(IsPublic: false);
        var updatePrivacyResponse = await Factory.ApiClient.PostAsync<UpdatePrivacyRequest, ApiResult.EmptyResult>(
            ApiRoutes.UserProfile.Privacy, request);
        updatePrivacyResponse.Success.ShouldBeTrue();

        // 4. User A can't see User B in feed
        Factory.ApiClient.SetAuth(authA);
        var feedAfter = await Factory.ApiClient.GetAsync<ApiResult<GetFeedResponse>>(ApiRoutes.Feed.GetFeed);
        feedAfter.Result.Feed.ShouldBeEmpty();
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

        // 3. User A skips User B
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
        var blockRequest = new BlockUserRequest(null);
        var response = await Factory.ApiClient.PostAsync<BlockUserRequest, ApiResult.EmptyResult>(
            ApiRoutes.Feed.BlockUser(userBId), blockRequest);
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
        var response = await Factory.ApiClient.PostAsync<InviteUserRequest, ApiResult.EmptyResult>(
            ApiRoutes.Feed.InviteUser(userBId),
            new InviteUserRequest(ComputeExpiresAtForNowOption())
        );

        response.Success.ShouldBeTrue();
    }

    [Fact]
    public async Task GivenTwoUsers_WhenCheckingInviteExpiration_ThenReturnsCorrectExpiration()
    {
        Factory.FakeTimeProvider.Reset(DateTime.UtcNow);

        // 1. Create two users
        var authA = await Factory.AuthManager.CreateAndLoginUserAsync("userA@example.com", "Password123!");
        Factory.ApiClient.SetAuth(authA);
        await Factory.AuthManager.CreateDefaultProfileAndSubmitLocation();

        var authB = await Factory.AuthManager.CreateAndLoginUserAsync("userB@example.com", "Password123!");
        var userBId = authB.UserId;
        Factory.ApiClient.SetAuth(authB);
        await Factory.AuthManager.CreateDefaultProfileAndSubmitLocation();

        // 2. Before any invite, User B checks invite expiration from User A — should be null
        Factory.ApiClient.SetAuth(authB);
        var beforeInvite = await Factory.ApiClient
            .GetAsync<ApiResult<GetInviteExpirationResponse>>(ApiRoutes.Feed.GetInviteExpiration(authA.UserId));
        beforeInvite.Success.ShouldBeTrue();
        beforeInvite.Result.InviteExpiresAt.ShouldBeNull();

        // 3. User A invites User B with a known expiration
        var expiresAt = DateTimeOffset.UtcNow.AddHours(24);
        Factory.ApiClient.SetAuth(authA);
        var inviteResponse = await Factory.ApiClient.PostAsync<InviteUserRequest, ApiResult.EmptyResult>(
            ApiRoutes.Feed.InviteUser(userBId), new InviteUserRequest(expiresAt));
        inviteResponse.Success.ShouldBeTrue();

        // 4. User B checks invite expiration from User A — should return expiration date
        Factory.ApiClient.SetAuth(authB);
        var afterInvite = await Factory.ApiClient
            .GetAsync<ApiResult<GetInviteExpirationResponse>>(ApiRoutes.Feed.GetInviteExpiration(authA.UserId));
        afterInvite.Success.ShouldBeTrue();
        afterInvite.Result.InviteExpiresAt.ShouldNotBeNull();
        afterInvite.Result.InviteExpiresAt.ShouldBe(expiresAt);

        // 5. Advance time past expiration — should return null
        Factory.FakeTimeProvider.SetUtcNow(expiresAt.AddSeconds(2));
        var afterExpiry = await Factory.ApiClient
            .GetAsync<ApiResult<GetInviteExpirationResponse>>(ApiRoutes.Feed.GetInviteExpiration(authA.UserId));
        afterExpiry.Success.ShouldBeTrue();
        afterExpiry.Result.InviteExpiresAt.ShouldBeNull();

        Factory.FakeTimeProvider.Reset(DateTime.UtcNow);
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
        await Factory.ApiClient.PostAsync<InviteUserRequest, ApiResult.EmptyResult>(
            ApiRoutes.Feed.InviteUser(authA.UserId),
            new InviteUserRequest(ComputeExpiresAtForNowOption()));
        await Factory.ApiClient.PostAsync<InviteUserRequest, ApiResult.EmptyResult>(
            ApiRoutes.Feed.InviteUser(authB.UserId),
            new InviteUserRequest(ComputeExpiresAtForNowOption()));

        // 3. A and B invite C simultaneously
        using var httpClientA = Factory.CreateClient();
        httpClientA.SetAuth(authA);
        var taskA = httpClientA.PostAsync(
            ApiRoutes.Feed.InviteUser(userCId),
            JsonContent.Create(new InviteUserRequest(ComputeExpiresAtForNowOption()))
        );

        using var httpClientB = Factory.CreateClient();
        httpClientB.SetAuth(authB);
        var taskB = httpClientA.PostAsync(
            ApiRoutes.Feed.InviteUser(userCId),
            JsonContent.Create(new InviteUserRequest(ComputeExpiresAtForNowOption()))
        );
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
    public async Task GivenUser_WhenSkipsMultipleUsers_ThenFeedIsRefilledWithNewUsers()
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
        var blockRequest = new BlockUserRequest(null);
        var response = await Factory.ApiClient.PostAsync<BlockUserRequest, ApiResult.EmptyResult>(
            ApiRoutes.Feed.BlockUser(userBId), blockRequest);
        response.Success.ShouldBeTrue();

        // 3. User A retrieves blocked users
        var blockedUsersResponse =
            await Factory.ApiClient.GetAsync<ApiResult<PaginatedResponse<BlockedUserDto>>>(ApiRoutes.Feed
                .GetBlockedUsers);
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
        var getResponse =
            await Factory.ApiClient.GetAsync<ApiResult<GetSettingsResponse>>(ApiRoutes.Feed.DisplaySettings);
        getResponse.Success.ShouldBeTrue();
        getResponse.Result.Settings.DistanceUnits.ShouldBe(DistanceUnits.Miles);
    }

    [Fact]
    public async Task GivenTwoUsers_WhenUserBlocksWithReportMessage_ThenMessageIsSaved()
    {
        // 1. Create two users and submit locations
        var authA = await Factory.AuthManager.CreateAndLoginUserAsync("reporter@example.com", "Password123!");
        Factory.ApiClient.SetAuth(authA);
        await Factory.AuthManager.CreateDefaultProfileAndSubmitLocation();

        var authB = await Factory.AuthManager.CreateAndLoginUserAsync("reported@example.com", "Password123!");
        var userBId = authB.UserId;
        Factory.ApiClient.SetAuth(authB);
        await Factory.AuthManager.CreateDefaultProfileAndSubmitLocation();

        // 2. User A blocks User B with a report message
        Factory.ApiClient.SetAuth(authA);
        const string reportMessage = "This user was sending spam messages";
        var blockRequest = new BlockUserRequest(reportMessage);
        var response = await Factory.ApiClient.PostAsync<BlockUserRequest, ApiResult.EmptyResult>(
            ApiRoutes.Feed.BlockUser(userBId), blockRequest);
        response.Success.ShouldBeTrue();

        // 3. Verify the report message is saved in the database
        using var scope = Factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IFeedUserRelationRepository>();
        var relation = await repository.GetAsync(Guid.Parse(authA.UserId), Guid.Parse(userBId), asNoTracking: true);

        relation.ShouldNotBeNull();
        relation.IsBlocked.ShouldBeTrue();
        relation.ReportMessage.ShouldBe(reportMessage);
    }

    [Fact]
    public async Task
        GivenUser_WhenInvitesMoreThanLimit_ThenReturnsTooManyRequests_AndReturnsDefaultCounterAfterWaitingForNewDay()
    {
        Factory.FakeTimeProvider.Reset(DateTime.UtcNow.Date);

        // 1. Create main user
        var authMain = await Factory.AuthManager.CreateAndLoginUserAsync("user@example.com", "Password123!");
        Factory.ApiClient.SetAuth(authMain);
        await Factory.AuthManager.CreateDefaultProfileAndSubmitLocation();

        // 2. Create FeedParameters.DefaultAvailableInvitesPerDay + 1 candidate users
        const int totalCandidates = FeedParameters.DefaultAvailableInvitesPerDay + 1;
        var candidateIds = new List<string>();
        for (var i = 0; i < totalCandidates; i++)
        {
            var authCandidate = await Factory.AuthManager
                .CreateAndLoginUserAsync($"limit_candidate{i}@example.com", "Password123!");
            candidateIds.Add(authCandidate.UserId);
            Factory.ApiClient.SetAuth(authCandidate);
            await Factory.AuthManager.CreateDefaultProfileAndSubmitLocation();
        }

        // 3. Invite FeedParameters.DefaultAvailableInvitesPerDay users successfully
        Factory.ApiClient.SetAuth(authMain);
        for (var i = 0; i < FeedParameters.DefaultAvailableInvitesPerDay; i++)
        {
            var response = await Factory.ApiClient.PostAsync<InviteUserRequest, ApiResult.EmptyResult>(
                ApiRoutes.Feed.InviteUser(candidateIds[i]),
                new InviteUserRequest(ComputeExpiresAtForNowOption())
            );
            response.Success.ShouldBeTrue();

            var currentStatus = await Factory.ApiClient
                .GetAsync<ApiResult<GetFeedStatusResponse>>(ApiRoutes.Feed.GetStatus);
            currentStatus.Success.ShouldBeTrue();
            currentStatus.Result.AvailableInvitesCount.ShouldBe(FeedParameters.DefaultAvailableInvitesPerDay - i - 1);
        }

        // 4. Try to invite one more user and expect failure
        var failedResponse = await Factory.ApiClient.PostAsync<InviteUserRequest, ApiResult.EmptyResult>(
            ApiRoutes.Feed.InviteUser(candidateIds[FeedParameters.DefaultAvailableInvitesPerDay]),
            new InviteUserRequest(ComputeExpiresAtForNowOption()));

        failedResponse.Success.ShouldBeFalse();
        failedResponse.Errors.First().Code.ShouldBe(nameof(ErrorCode.TooManyRequests));

        // 5. Wait until midnight and check limit
        var today23H = Factory.FakeTimeProvider.GetUtcNow().Date.AddDays(1).AddHours(-1);
        Factory.FakeTimeProvider.SetUtcNow(today23H);

        var today23HStatus = await Factory.ApiClient
            .GetAsync<ApiResult<GetFeedStatusResponse>>(ApiRoutes.Feed.GetStatus);
        today23HStatus.Success.ShouldBeTrue();
        today23HStatus.Result.AvailableInvitesCount.ShouldBe(0);

        // 6. Wait until midnight and check limit
        var tomorrow = Factory.FakeTimeProvider.GetUtcNow().Date.AddDays(1);
        Factory.FakeTimeProvider.SetUtcNow(tomorrow);

        var tomorrowStatus = await Factory.ApiClient
            .GetAsync<ApiResult<GetFeedStatusResponse>>(ApiRoutes.Feed.GetStatus);
        tomorrowStatus.Success.ShouldBeTrue();
        tomorrowStatus.Result.AvailableInvitesCount.ShouldBe(FeedParameters.DefaultAvailableInvitesPerDay);

        Factory.FakeTimeProvider.Reset(DateTime.UtcNow);
    }

    [Fact]
    public async Task
        GivenInvitedUser_WhenThisUserAcceptsDifferentInvite_ThenAvailableInvitesCountReverted()
    {
        // 1. Create users
        var authMain = await Factory.AuthManager.CreateAndLoginUserAsync("user@example.com", "Password123!");
        Factory.ApiClient.SetAuth(authMain);
        await Factory.AuthManager.CreateDefaultProfileAndSubmitLocation();

        var authA = await Factory.AuthManager.CreateAndLoginUserAsync("user_a@example.com", "Password123!");
        Factory.ApiClient.SetAuth(authA);
        await Factory.AuthManager.CreateDefaultProfileAndSubmitLocation();

        var authB = await Factory.AuthManager.CreateAndLoginUserAsync("user_b@example.com", "Password123!");
        Factory.ApiClient.SetAuth(authB);
        await Factory.AuthManager.CreateDefaultProfileAndSubmitLocation();

        // 2. Main invites A
        Factory.ApiClient.SetAuth(authMain);
        var response = await Factory.ApiClient.PostAsync<InviteUserRequest, ApiResult.EmptyResult>(
            ApiRoutes.Feed.InviteUser(authA.UserId),
            new InviteUserRequest(ComputeExpiresAtForNowOption())
        );
        response.Success.ShouldBeTrue();

        var currentStatus = await Factory.ApiClient
            .GetAsync<ApiResult<GetFeedStatusResponse>>(ApiRoutes.Feed.GetStatus);
        currentStatus.Success.ShouldBeTrue();
        currentStatus.Result.AvailableInvitesCount.ShouldBe(FeedParameters.DefaultAvailableInvitesPerDay - 1);

        // 3. Users A and B establish meeting
        await Factory.Host.ExecuteAndWaitAsync(async () =>
        {
            Factory.ApiClient.SetAuth(authA);
            var inviteAResponse = await Factory.ApiClient.PostAsync<InviteUserRequest, ApiResult.EmptyResult>(
                ApiRoutes.Feed.InviteUser(authB.UserId),
                new InviteUserRequest(ComputeExpiresAtForNowOption())
            );
            inviteAResponse.Success.ShouldBeTrue();

            Factory.ApiClient.SetAuth(authB);
            var inviteBResponse = await Factory.ApiClient.PostAsync<InviteUserRequest, ApiResult.EmptyResult>(
                ApiRoutes.Feed.InviteUser(authA.UserId),
                new InviteUserRequest(ComputeExpiresAtForNowOption())
            );
            inviteBResponse.Success.ShouldBeTrue();
        });

        // 4.Check available invites
        Factory.ApiClient.SetAuth(authMain);
        var currentStatusAfter = await Factory.ApiClient
            .GetAsync<ApiResult<GetFeedStatusResponse>>(ApiRoutes.Feed.GetStatus);
        currentStatusAfter.Success.ShouldBeTrue();
        currentStatusAfter.Result.AvailableInvitesCount.ShouldBe(FeedParameters.DefaultAvailableInvitesPerDay);
    }

    [Fact]
    public async Task
       GivenInvitedUser_WhenThisUserBecomesPrivate_ThenAvailableInvitesCountReverted()
    {
        // 1. Create users
        var authA = await Factory.AuthManager.CreateAndLoginUserAsync("_user_a@example.com", "Password123!");
        Factory.ApiClient.SetAuth(authA);
        await Factory.AuthManager.CreateDefaultProfileAndSubmitLocation();

        var authB = await Factory.AuthManager.CreateAndLoginUserAsync("_user_b@example.com", "Password123!");
        Factory.ApiClient.SetAuth(authB);
        await Factory.AuthManager.CreateDefaultProfileAndSubmitLocation();

        // 2. A invites B
        Factory.ApiClient.SetAuth(authA);
        var response = await Factory.ApiClient.PostAsync<InviteUserRequest, ApiResult.EmptyResult>(
            ApiRoutes.Feed.InviteUser(authB.UserId),
            new InviteUserRequest(ComputeExpiresAtForNowOption())
        );
        response.Success.ShouldBeTrue();

        // 3. Users B changes visibility
        Factory.ApiClient.SetAuth(authB);
        var request = new UpdatePrivacyRequest(IsPublic: false);
        var updatePrivacyResponse = await Factory.ApiClient
            .PostAsync<UpdatePrivacyRequest, ApiResult.EmptyResult>(ApiRoutes.UserProfile.Privacy, request);
        updatePrivacyResponse.Success.ShouldBeTrue();

        // 4. Check available invites
        Factory.ApiClient.SetAuth(authA);
        var currentStatusAfter = await Factory.ApiClient
            .GetAsync<ApiResult<GetFeedStatusResponse>>(ApiRoutes.Feed.GetStatus);
        currentStatusAfter.Success.ShouldBeTrue();
        currentStatusAfter.Result.AvailableInvitesCount.ShouldBe(FeedParameters.DefaultAvailableInvitesPerDay);
    }

    [Fact]
    public async Task GivenTwoUsers_WhenMeetingEstablished_ThenBothUsersAppearInFriendsList()
    {
        // 1. Create two users and submit locations
        var authA = await Factory.AuthManager.CreateAndLoginUserAsync("friend_a@example.com", "Password123!");
        Factory.ApiClient.SetAuth(authA);
        await Factory.AuthManager.CreateDefaultProfileAndSubmitLocation();

        var authB = await Factory.AuthManager.CreateAndLoginUserAsync("friend_b@example.com", "Password123!");
        var userBId = authB.UserId;
        Factory.ApiClient.SetAuth(authB);
        await Factory.AuthManager.CreateDefaultProfileAndSubmitLocation();

        // 2. Establish a meeting (mutual invite)
        Factory.ApiClient.SetAuth(authA);
        var inviteResponse = await Factory.ApiClient.PostAsync<InviteUserRequest, ApiResult.EmptyResult>(
            ApiRoutes.Feed.InviteUser(userBId),
            new InviteUserRequest(ComputeExpiresAtForNowOption()));
        inviteResponse.Success.ShouldBeTrue();

        Factory.ApiClient.SetAuth(authB);
        var inviteBackResponse = await Factory.ApiClient.PostAsync<InviteUserRequest, ApiResult.EmptyResult>(
            ApiRoutes.Feed.InviteUser(authA.UserId),
            new InviteUserRequest(ComputeExpiresAtForNowOption()));
        inviteBackResponse.Success.ShouldBeTrue();

        // 3. User A's friends list should contain User B
        Factory.ApiClient.SetAuth(authA);
        var friendsA = await Factory.ApiClient.GetAsync<ApiResult<PaginatedResponse<FriendUserDto>>>(
            ApiRoutes.Feed.GetFriends);
        friendsA.Success.ShouldBeTrue();
        friendsA.Result.Data.ShouldNotBeEmpty();
        friendsA.Result.Data.Any(f => f.Id == Guid.Parse(userBId)).ShouldBeTrue();

        // 4. User B's friends list should contain User A
        Factory.ApiClient.SetAuth(authB);
        var friendsB = await Factory.ApiClient.GetAsync<ApiResult<PaginatedResponse<FriendUserDto>>>(
            ApiRoutes.Feed.GetFriends);
        friendsB.Success.ShouldBeTrue();
        friendsB.Result.Data.ShouldNotBeEmpty();
        friendsB.Result.Data.Any(f => f.Id == Guid.Parse(authA.UserId)).ShouldBeTrue();
    }

    [Fact]
    public async Task GivenTwoUsers_WhenUserAcceptsInviteWithNoAvailableInvites_ThenBothBecameFriends()
    {
        Factory.FakeTimeProvider.Reset(DateTime.UtcNow.Date);

        // 1. Create User A
        var authA = await Factory.AuthManager.CreateAndLoginUserAsync("accept_a@example.com", "Password123!");
        Factory.ApiClient.SetAuth(authA);
        await Factory.AuthManager.CreateDefaultProfileAndSubmitLocation();

        // 2. Create User B
        var authB = await Factory.AuthManager.CreateAndLoginUserAsync("accept_b@example.com", "Password123!");
        var userBId = authB.UserId;
        Factory.ApiClient.SetAuth(authB);
        await Factory.AuthManager.CreateDefaultProfileAndSubmitLocation();

        // 3. Create enough candidate users for User B to exhaust all invites
        var candidateIds = new List<string>();
        for (var i = 0; i < FeedParameters.DefaultAvailableInvitesPerDay; i++)
        {
            var authCandidate = await Factory.AuthManager
                .CreateAndLoginUserAsync($"accept_candidate{i}@example.com", "Password123!");
            candidateIds.Add(authCandidate.UserId);
            Factory.ApiClient.SetAuth(authCandidate);
            await Factory.AuthManager.CreateDefaultProfileAndSubmitLocation();
        }

        // 4. User B exhausts all available invites
        Factory.ApiClient.SetAuth(authB);
        foreach (var candidateId in candidateIds)
        {
            var response = await Factory.ApiClient.PostAsync<InviteUserRequest, ApiResult.EmptyResult>(
                ApiRoutes.Feed.InviteUser(candidateId),
                new InviteUserRequest(ComputeExpiresAtForNowOption()));
            response.Success.ShouldBeTrue();
        }

        // 5. Confirm User B has 0 available invites
        var status = await Factory.ApiClient.GetAsync<ApiResult<GetFeedStatusResponse>>(ApiRoutes.Feed.GetStatus);
        status.Result.AvailableInvitesCount.ShouldBe(0);

        // 6. User A invites User B
        Factory.ApiClient.SetAuth(authA);
        var inviteResponse = await Factory.ApiClient.PostAsync<InviteUserRequest, ApiResult.EmptyResult>(
            ApiRoutes.Feed.InviteUser(userBId),
            new InviteUserRequest(ComputeExpiresAtForNowOption()));
        inviteResponse.Success.ShouldBeTrue();

        // 7. User B accepts the invite despite having 0 available invites
        Factory.ApiClient.SetAuth(authB);
        var acceptResponse = await Factory.ApiClient.PostAsync<object?, ApiResult.EmptyResult>(
            ApiRoutes.Feed.AcceptInvite(authA.UserId), null);
        acceptResponse.Success.ShouldBeTrue();

        // 8. Both users should be friends
        Factory.ApiClient.SetAuth(authA);
        var friendsA = await Factory.ApiClient.GetAsync<ApiResult<PaginatedResponse<FriendUserDto>>>(
            ApiRoutes.Feed.GetFriends);
        friendsA.Result.Data.Any(f => f.Id == Guid.Parse(userBId)).ShouldBeTrue();

        Factory.ApiClient.SetAuth(authB);
        var friendsB = await Factory.ApiClient.GetAsync<ApiResult<PaginatedResponse<FriendUserDto>>>(
            ApiRoutes.Feed.GetFriends);
        friendsB.Result.Data.Any(f => f.Id == Guid.Parse(authA.UserId)).ShouldBeTrue();

        Factory.FakeTimeProvider.Reset(DateTime.UtcNow);
    }

    [Fact]
    public async Task GivenTwoUsers_WhenUserAcceptsExpiredInvite_ThenReturnsError()
    {
        Factory.FakeTimeProvider.Reset(DateTime.UtcNow);

        // 1. Create two users and submit locations
        var authA = await Factory.AuthManager.CreateAndLoginUserAsync("expired_a@example.com", "Password123!");
        Factory.ApiClient.SetAuth(authA);
        await Factory.AuthManager.CreateDefaultProfileAndSubmitLocation();

        var authB = await Factory.AuthManager.CreateAndLoginUserAsync("expired_b@example.com", "Password123!");
        var userBId = authB.UserId;
        Factory.ApiClient.SetAuth(authB);
        await Factory.AuthManager.CreateDefaultProfileAndSubmitLocation();

        // 2. User A invites User B
        Factory.ApiClient.SetAuth(authA);
        var expiresAt = ComputeExpiresAtForNowOption();
        var inviteResponse = await Factory.ApiClient.PostAsync<InviteUserRequest, ApiResult.EmptyResult>(
            ApiRoutes.Feed.InviteUser(userBId),
            new InviteUserRequest(expiresAt));
        inviteResponse.Success.ShouldBeTrue();

        // 3. Time passes beyond the invite expiration
        Factory.FakeTimeProvider.SetUtcNow(expiresAt.AddSeconds(2));

        // 4. User B tries to accept the expired invite
        Factory.ApiClient.SetAuth(authB);
        var acceptResponse = await Factory.ApiClient.PostAsync<object?, ApiResult.EmptyResult>(
            ApiRoutes.Feed.AcceptInvite(authA.UserId), null);
        acceptResponse.Success.ShouldBeFalse();
        acceptResponse.Errors.First().Code.ShouldBe(nameof(ErrorCode.ValidationError));

        // 5. Neither user should be in the other's friends list
        Factory.ApiClient.SetAuth(authA);
        var friendsA = await Factory.ApiClient.GetAsync<ApiResult<PaginatedResponse<FriendUserDto>>>(
            ApiRoutes.Feed.GetFriends);
        friendsA.Result.Data.Any(f => f.Id == Guid.Parse(userBId)).ShouldBeFalse();

        Factory.FakeTimeProvider.Reset(DateTime.UtcNow);
    }

    [Fact]
    public async Task GivenFriends_WhenUserRemovesFriend_ThenFriendDisappearsFromList()
    {
        // 1. Create two users and submit locations
        var authA = await Factory.AuthManager.CreateAndLoginUserAsync("remover_a@example.com", "Password123!");
        Factory.ApiClient.SetAuth(authA);
        await Factory.AuthManager.CreateDefaultProfileAndSubmitLocation();

        var authB = await Factory.AuthManager.CreateAndLoginUserAsync("remover_b@example.com", "Password123!");
        var userBId = authB.UserId;
        Factory.ApiClient.SetAuth(authB);
        await Factory.AuthManager.CreateDefaultProfileAndSubmitLocation();

        // 2. Establish a meeting (mutual invite)
        Factory.ApiClient.SetAuth(authA);
        await Factory.ApiClient.PostAsync<InviteUserRequest, ApiResult.EmptyResult>(
            ApiRoutes.Feed.InviteUser(userBId),
            new InviteUserRequest(ComputeExpiresAtForNowOption()));

        Factory.ApiClient.SetAuth(authB);
        await Factory.ApiClient.PostAsync<InviteUserRequest, ApiResult.EmptyResult>(
            ApiRoutes.Feed.InviteUser(authA.UserId),
            new InviteUserRequest(ComputeExpiresAtForNowOption()));

        // 3. Verify User B is in User A's friends list
        Factory.ApiClient.SetAuth(authA);
        var friendsBefore = await Factory.ApiClient.GetAsync<ApiResult<PaginatedResponse<FriendUserDto>>>(
            ApiRoutes.Feed.GetFriends);
        friendsBefore.Result.Data.Any(f => f.Id == Guid.Parse(userBId)).ShouldBeTrue();

        // 4. User A removes User B from friends
        var removeResponse = await Factory.ApiClient.PostAsync<object?, ApiResult.EmptyResult>(
            ApiRoutes.Feed.RemoveFriend(userBId), null);
        removeResponse.Success.ShouldBeTrue();

        // 5. User B no longer appears in User A's friends list
        var friendsAfter = await Factory.ApiClient.GetAsync<ApiResult<PaginatedResponse<FriendUserDto>>>(
            ApiRoutes.Feed.GetFriends);
        friendsAfter.Result.Data.Any(f => f.Id == Guid.Parse(userBId)).ShouldBeFalse();
    }

    [Fact]
    public async Task GivenTwoUsers_WhenUserInvitesFriendWithNoAvailableInvites_ThenSentInvite()
    {
        // 1. Create user A and user B
        var authA = await Factory.AuthManager.CreateAndLoginUserAsync("invite_friend_a@example.com", "Password123!");
        var userAId = authA.UserId;
        Factory.ApiClient.SetAuth(authA);
        await Factory.AuthManager.CreateDefaultProfileAndSubmitLocation();

        var authB = await Factory.AuthManager.CreateAndLoginUserAsync("invite_friend_b@example.com", "Password123!");
        var userBId = authB.UserId;
        Factory.ApiClient.SetAuth(authB);
        await Factory.AuthManager.CreateDefaultProfileAndSubmitLocation();

        // 2. Make them friends
        // It's easier to do this via the database; otherwise, we would have to invite each other,
        // delete the chat to reset the isInMeeting flags, and wait for notifications
        // to ensure that the entire sequence has finished. 
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FeedContext>();
        var relAToB = new FeedUserRelation
        {
            Id = Guid.CreateVersion7(),
            MyUserId = Guid.Parse(userAId),
            OtherUserId = Guid.Parse(userBId),
            InviteExpiresAt = default,
            InvitedAt = default,
            SkippedAt = default,
            LastSeenAt = default,
            IsFriend = true,
        };
        var relBToA = new FeedUserRelation
        {
            Id = Guid.CreateVersion7(),
            MyUserId = Guid.Parse(userBId),
            OtherUserId = Guid.Parse(userAId),
            InviteExpiresAt = default,
            InvitedAt = default,
            SkippedAt = default,
            LastSeenAt = default,
            IsFriend = true,
        };
        context.FeedUserRelations.AddRange(relAToB, relBToA);
        await context.SaveChangesAsync();

        // 3. Create enough candidate users for User B to exhaust all invites
        Factory.ApiClient.SetAuth(authB);
        var candidateIds = new List<string>();
        for (var i = 0; i < FeedParameters.DefaultAvailableInvitesPerDay; i++)
        {
            var authCandidate = await Factory.AuthManager
                .CreateAndLoginUserAsync($"accept_candidate{i}@example.com", "Password123!");
            candidateIds.Add(authCandidate.UserId);
            Factory.ApiClient.SetAuth(authCandidate);
            await Factory.AuthManager.CreateDefaultProfileAndSubmitLocation();
        }

        // 4. User B exhausts all available invites
        Factory.ApiClient.SetAuth(authB);
        foreach (var candidateId in candidateIds)
        {
            var response = await Factory.ApiClient.PostAsync<InviteUserRequest, ApiResult.EmptyResult>(
                ApiRoutes.Feed.InviteUser(candidateId),
                new InviteUserRequest(ComputeExpiresAtForNowOption()));
            response.Success.ShouldBeTrue();
        }

        // 5. Confirm User B has 0 available invites
        var status = await Factory.ApiClient.GetAsync<ApiResult<GetFeedStatusResponse>>(ApiRoutes.Feed.GetStatus);
        status.Result.AvailableInvitesCount.ShouldBe(0);

        // 6. User B invites User A
        await Factory.Host.ExecuteAndWaitAsync(async () =>
        {
            Factory.ApiClient.SetAuth(authB);
            var inviteResponse = await Factory.ApiClient.PostAsync<InviteUserRequest, ApiResult.EmptyResult>(
                ApiRoutes.Feed.InviteFriend(userAId),
                new InviteUserRequest(ComputeExpiresAtForNowOption()));
            inviteResponse.Success.ShouldBeTrue();
        });

        // 7. User A received the invite in notifications
        Factory.ApiClient.SetAuth(authA);
        var notificationsResponse = await Factory.ApiClient.GetAsync<ApiResult<PaginatedResponse<NotificationDto>>>(
            ApiRoutes.Notifications.GetNotificationsPaginated(0, 10));
        notificationsResponse.Success.ShouldBeTrue();
        notificationsResponse.Result.Data.Count.ShouldBe(1);
        notificationsResponse.Result.Data.First().FromUserId.ShouldBe(Guid.Parse(userBId));
    }

    [Fact]
    public async Task GivenNonFriendUser_WhenInvitingViaFriendEndpoint_ThenReturnForbidden()
    {
        // 1. Create User A and User B (Non-Friend)
        var authA = await Factory.AuthManager.CreateAndLoginUserAsync("a@example.com", "Password123!");
        Factory.ApiClient.SetAuth(authA);
        await Factory.AuthManager.CreateDefaultProfileAndSubmitLocation();

        var authB = await Factory.AuthManager.CreateAndLoginUserAsync("b@example.com", "Password123!");
        var userBId = authB.UserId;

        // 2. User A tries to invite User C via Friend Endpoint
        Factory.ApiClient.SetAuth(authA);
        var request = new InviteUserRequest(DateTimeOffset.UtcNow.AddHours(1));
        var response = await Factory.ApiClient
            .PostAsync<InviteUserRequest, ApiResult.EmptyResult>(ApiRoutes.Feed.InviteFriend(userBId), request);

        response.Success.ShouldBeFalse();
        response.Errors.ShouldContain(e => e.Code == "ValidationError");
        response.Errors.ShouldContain(e => e.Error.Contains("not a friend"));
    }

    [Fact]
    public async Task GivenBlockedUser_WhenTheyInvite_ThenInviteIsNotSent()
    {
        // 1. Create users
        var authA = await Factory.AuthManager.CreateAndLoginUserAsync("normalUserA@example.com", "Password123!");
        Factory.ApiClient.SetAuth(authA);
        await Factory.AuthManager.CreateDefaultProfileAndSubmitLocation();

        var authB = await Factory.AuthManager.CreateAndLoginUserAsync("blockedUserB@example.com", "Password123!");
        Factory.ApiClient.SetAuth(authB);
        await Factory.AuthManager.CreateDefaultProfileAndSubmitLocation();

        // 2. Create device tokens for push notifications
        var tokenRequestA = new UpdateFcmTokenRequest("devA", "tokenA");
        Factory.ApiClient.SetAuth(authA);
        await Factory.ApiClient
            .PostAsync<UpdateFcmTokenRequest, ApiResult.EmptyResult>(ApiRoutes.Notifications.UpdateDeviceToken,
                tokenRequestA);

        var tokenRequestB = new UpdateFcmTokenRequest("devB", "tokenB");
        Factory.ApiClient.SetAuth(authB);
        await Factory.ApiClient
            .PostAsync<UpdateFcmTokenRequest, ApiResult.EmptyResult>(ApiRoutes.Notifications.UpdateDeviceToken,
                tokenRequestB);

        // 3. User A blocks User B
        Factory.ApiClient.SetAuth(authA);
        var blockRequest = new BlockUserRequest(null);
        var response = await Factory.ApiClient.PostAsync<BlockUserRequest, ApiResult.EmptyResult>(
            ApiRoutes.Feed.BlockUser(authB.UserId), blockRequest);
        response.Success.ShouldBeTrue();

        // 4. User B invites User A
        await Factory.Host.ExecuteAndWaitAsync(async () =>
        {
            var inviteExpiresAt = Factory.FakeTimeProvider.GetUtcNow().AddHours(4);
            Factory.ApiClient.SetAuth(authB);
            await Factory.ApiClient
                .PostAsync<InviteUserRequest, ApiResult.EmptyResult>(
                    ApiRoutes.Feed.InviteUser(authA.UserId),
                    new InviteUserRequest(inviteExpiresAt)
                );
        });

        // 5. Check that notification list for user A is empty
        Factory.ApiClient.SetAuth(authA);
        var notificationsResponse = await Factory.ApiClient.GetAsync<ApiResult<PaginatedResponse<NotificationDto>>>(
            ApiRoutes.Notifications.GetNotificationsPaginated(0, 10));
        notificationsResponse.Success.ShouldBeTrue();
        notificationsResponse.Result.TotalCount.ShouldBe(0);
    }
}