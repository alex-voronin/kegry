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
        await Factory.ApiClient.PostAsync<UpdateFcmTokenRequest>(
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
            .GetAsync<PaginatedResponse<FriendUserDto>>(ApiRoutes.Feed.GetFriends);
        friendsA.TotalCount.ShouldBe(1);
        friendsA.Data.First().Id.ShouldBe(userBId);

        Factory.ApiClient.SetAuth(authB);
        var friendsB = await Factory.ApiClient
            .GetAsync<PaginatedResponse<FriendUserDto>>(ApiRoutes.Feed.GetFriends);
        friendsB.TotalCount.ShouldBe(1);
        friendsB.Data.First().Id.ShouldBe(userAId);

        // 4. Verify user A has a secret npc and received a push
        Factory.ApiClient.SetAuth(authA);
        var metNpcsA = await Factory.ApiClient.GetAsync<GetMetNpcsResponse>(ApiRoutes.Npc.MetNpcs);
        metNpcsA.MetNpcs.Length.ShouldBe(1);
        metNpcsA.MetNpcs.First().IsSecret.ShouldBeTrue();

        // 4. Verify user B has no npcs
        Factory.ApiClient.SetAuth(authB);
        var metNpcsB = await Factory.ApiClient.GetAsync<GetMetNpcsResponse>(ApiRoutes.Npc.MetNpcs);
        metNpcsB.MetNpcs.Length.ShouldBe(0);
    }
}