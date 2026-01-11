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
            .PostAsync<UpdateFcmTokenRequest, ApiResult.EmptyResult>("/api/v1/notifications/device/token", requestA);
        tokenResponseA.Success.ShouldBeTrue();

        var requestB = new UpdateFcmTokenRequest("devB", "tokenB");
        _factory.ApiClient.SetAuth(authResponseB);
        var tokenResponseB = await _factory.ApiClient
            .PostAsync<UpdateFcmTokenRequest, ApiResult.EmptyResult>("/api/v1/notifications/device/token", requestB);
        tokenResponseB.Success.ShouldBeTrue();

        // 3. Send invites
        _factory.ApiClient.SetAuth(authResponseA);
        var inviteResponseA = await _factory.ApiClient
            .PostAsync<object?, ApiResult.EmptyResult>($"/api/v1/feed/{userBId}/invite", null);
        inviteResponseA.Success.ShouldBeTrue();

        _factory.ApiClient.SetAuth(authResponseB);
        var inviteResponseB = await _factory.ApiClient
            .PostAsync<object?, ApiResult.EmptyResult>($"/api/v1/feed/{userAId}/invite", null);
        inviteResponseB.Success.ShouldBeTrue();

        // 4. Setup push notifications awaiting
        const int expectedNotificationsCount = 2;
        await _factory.NotificationManager.AwaitPushNotifications(expectedNotificationsCount, "Invitation accepted!");
    }
}