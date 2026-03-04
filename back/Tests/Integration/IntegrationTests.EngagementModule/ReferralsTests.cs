using EngagementModule.API.Dto;
using FeedModule.API.Dto;
using IntegrationTests.Shared;
using NotificationsModule.API.Dto;
using NotificationsModule.Infrastructure.Transport.Push;
using SharedCore.Api.Dto;
using Shouldly;

namespace IntegrationTests.EngagementModule;

[Collection("IntegrationTests")]
public class ReferralsTests : IntegrationTestBase, IAsyncLifetime
{
    public ReferralsTests(IntegrationWebApplicationFactory factory) : base(factory)
    {
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public async Task DisposeAsync() => await ContainerManager.ResetDatabaseAsync();

    [Fact]
    public async Task GivenUser_WhenRefersAnotherUser_ThenReceivesSecretNpc_AndUsersAreFriends()
    {
        // 1. Create referrer user A and submit token for FCM
        var authA = await Factory.AuthManager.CreateAndLoginUserAsync("userA@example.com", "Password123!");
        Factory.ApiClient.SetAuth(authA);
        await Factory.AuthManager.CreateDefaultProfileAndSubmitLocation();
        var userAId = Guid.Parse(authA.UserId);

        var deviceId = "device-123";
        var fcmToken = "fcm-token-123";
        var request = new UpdateFcmTokenRequest(deviceId, fcmToken);
        var response = await Factory.ApiClient.PostAsync<UpdateFcmTokenRequest, ApiResult.EmptyResult>(
            ApiRoutes.Notifications.UpdateDeviceToken, request);

        // 2. Create referral user B
        var authB = await Factory.AuthManager.CreateAndLoginUserAsync("userB@example.com", "Password123!");
        var userBId = Guid.Parse(authB.UserId);
        await Factory.NotificationManager.AwaitPushNotifications(
            codeToRun: async () =>
            {
                Factory.ApiClient.SetAuth(authB);
                await Factory.AuthManager.CreateDefaultProfileAndSubmitLocation(referrerId: userAId);
            },
            expectedCount: 1,
            expectedType: FcmPushBuilder.PushType.ReferrerRewardReceived);

        // 3. Verify users are friends
        Factory.ApiClient.SetAuth(authA);
        var friendsA = await Factory.ApiClient
            .GetAsync<ApiResult<PaginatedResponse<FriendUserDto>>>(ApiRoutes.Feed.GetFriends);
        friendsA.Success.ShouldBeTrue();
        friendsA.Result.TotalCount.ShouldBe(1);
        friendsA.Result.Data.First().Id.ShouldBe(userBId);

        Factory.ApiClient.SetAuth(authB);
        var friendsB = await Factory.ApiClient
            .GetAsync<ApiResult<PaginatedResponse<FriendUserDto>>>(ApiRoutes.Feed.GetFriends);
        friendsB.Success.ShouldBeTrue();
        friendsB.Result.TotalCount.ShouldBe(1);
        friendsB.Result.Data.First().Id.ShouldBe(userAId);

        // 4. Verify user A has a secret npc and received a push
        Factory.ApiClient.SetAuth(authA);
        var metNpcsA = await Factory.ApiClient.GetAsync<ApiResult<GetMetNpcsResponse>>(ApiRoutes.Npc.MetNpcs);
        metNpcsA.Success.ShouldBeTrue();
        metNpcsA.Result.MetNpcs.Length.ShouldBe(1);
        metNpcsA.Result.MetNpcs.First().IsSecret.ShouldBeTrue();

        // 4. Verify user B has no npcs
        Factory.ApiClient.SetAuth(authB);
        var metNpcsB = await Factory.ApiClient.GetAsync<ApiResult<GetMetNpcsResponse>>(ApiRoutes.Npc.MetNpcs);
        metNpcsB.Success.ShouldBeTrue();
        metNpcsB.Result.MetNpcs.Length.ShouldBe(0);
    }
}