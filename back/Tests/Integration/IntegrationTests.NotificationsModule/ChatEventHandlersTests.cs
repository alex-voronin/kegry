using IntegrationTests.Shared;
using NotificationsModule.API;
using NotificationsModule.Infrastructure.Transport.Push;
using NSubstitute;
using SharedCore.Api.Dto;
using Shouldly;

namespace IntegrationTests.NotificationsModule;

[Collection("IntegrationTests")]
public class ChatEventHandlersTests : IAsyncLifetime
{
    private readonly IntegrationWebApplicationFactory _factory;

    public ChatEventHandlersTests(IntegrationWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public Task DisposeAsync() => _factory.ResetDatabaseAsync();

    [Fact]
    public async Task GivenTwoUsers_WhenTheyCreateChat_ThenTheyBothReceiveNotification()
    {
        // 1. Create users
        var authResponseA = await _factory.AuthManager.CreateAndLoginUserAsync("userA@example.com", "Password123!");
        var userAId = authResponseA.UserId;
        _factory.ApiClient.SetAuth(authResponseA);
        await _factory.AuthManager.CreateDefaultProfileAndSubmitLocation();

        var authResponseB = await _factory.AuthManager.CreateAndLoginUserAsync("userB@example.com", "Password123!");
        var userBId = authResponseB.UserId;
        _factory.ApiClient.SetAuth(authResponseB);
        await _factory.AuthManager.CreateDefaultProfileAndSubmitLocation();

        // 2. Create users' device tokens. They are required to send pushes
        var requestA = new UpdateFcmTokenRequest("devA", "tokenA");
        _factory.ApiClient.SetAuth(authResponseA);
        var tokenResponseA = await _factory.ApiClient
            .PostAsync<UpdateFcmTokenRequest, ApiResult.EmptyResult>(ApiRoutes.Notifications.UpdateDeviceToken,
                requestA);
        tokenResponseA.Success.ShouldBeTrue();

        var requestB = new UpdateFcmTokenRequest("devB", "tokenB");
        _factory.ApiClient.SetAuth(authResponseB);
        var tokenResponseB = await _factory.ApiClient
            .PostAsync<UpdateFcmTokenRequest, ApiResult.EmptyResult>(ApiRoutes.Notifications.UpdateDeviceToken,
                requestB);
        tokenResponseB.Success.ShouldBeTrue();

        // 3. Send invites and await push notifications
        await _factory.NotificationManager.AwaitPushNotifications(
            codeToRun: async () =>
            {
                _factory.ApiClient.SetAuth(authResponseA);
                var inviteResponseA = await _factory.ApiClient
                    .PostAsync<object?, ApiResult.EmptyResult>(ApiRoutes.Feed.InviteUser(userBId), null);
                inviteResponseA.Success.ShouldBeTrue();

                _factory.ApiClient.SetAuth(authResponseB);
                var inviteResponseB = await _factory.ApiClient
                    .PostAsync<object?, ApiResult.EmptyResult>(ApiRoutes.Feed.InviteUser(userAId), null);
                inviteResponseB.Success.ShouldBeTrue();
            },
            expectedCount: 2,
            expectedTitle: "Invitation accepted!");
    }
}