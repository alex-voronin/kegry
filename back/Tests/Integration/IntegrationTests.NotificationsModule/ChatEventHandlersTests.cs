using IntegrationTests.Shared;
using IntegrationTests.Shared.Managers;
using NotificationsModule.API;
using SharedCore.Api.Dto;
using Shouldly;

namespace IntegrationTests.NotificationsModule;

[Collection("IntegrationTests")]
public class ChatEventHandlersTests : IntegrationTestBase, IAsyncLifetime
{
    public ChatEventHandlersTests(IntegrationWebApplicationFactory factory) : base(factory)
    {
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public Task DisposeAsync() => ContainerManager.ResetDatabaseAsync();

    [Fact]
    public async Task GivenTwoUsers_WhenTheyCreateChat_ThenTheyBothReceiveNotification()
    {
        // 1. Create users
        var authResponseA = await Factory.AuthManager.CreateAndLoginUserAsync("userA@example.com", "Password123!");
        var userAId = authResponseA.UserId;
        Factory.ApiClient.SetAuth(authResponseA);
        await Factory.AuthManager.CreateDefaultProfileAndSubmitLocation();

        var authResponseB = await Factory.AuthManager.CreateAndLoginUserAsync("userB@example.com", "Password123!");
        var userBId = authResponseB.UserId;
        Factory.ApiClient.SetAuth(authResponseB);
        await Factory.AuthManager.CreateDefaultProfileAndSubmitLocation();

        // 2. Create users' device tokens. They are required to send pushes
        var requestA = new UpdateFcmTokenRequest("devA", "tokenA");
        Factory.ApiClient.SetAuth(authResponseA);
        var tokenResponseA = await Factory.ApiClient
            .PostAsync<UpdateFcmTokenRequest, ApiResult.EmptyResult>(ApiRoutes.Notifications.UpdateDeviceToken,
                requestA);
        tokenResponseA.Success.ShouldBeTrue();

        var requestB = new UpdateFcmTokenRequest("devB", "tokenB");
        Factory.ApiClient.SetAuth(authResponseB);
        var tokenResponseB = await Factory.ApiClient
            .PostAsync<UpdateFcmTokenRequest, ApiResult.EmptyResult>(ApiRoutes.Notifications.UpdateDeviceToken,
                requestB);
        tokenResponseB.Success.ShouldBeTrue();

        // 3. Send invites and await push notifications
        await Factory.NotificationManager.AwaitPushNotifications(
            codeToRun: async () =>
            {
                Factory.ApiClient.SetAuth(authResponseA);
                var inviteResponseA = await Factory.ApiClient
                    .PostAsync<object?, ApiResult.EmptyResult>(ApiRoutes.Feed.InviteUser(userBId), null);
                inviteResponseA.Success.ShouldBeTrue();

                Factory.ApiClient.SetAuth(authResponseB);
                var inviteResponseB = await Factory.ApiClient
                    .PostAsync<object?, ApiResult.EmptyResult>(ApiRoutes.Feed.InviteUser(userAId), null);
                inviteResponseB.Success.ShouldBeTrue();
            },
            expectedCount: 2,
            expectedTitle: "Invitation accepted!");
    }
}