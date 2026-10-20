using FeedModule.API.Dto;
using IntegrationTests.Shared;
using NotificationsModule.API.Dto;
using NotificationsModule.Infrastructure.Transport.Push;
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
        await Factory.ApiClient
            .PostAsync<UpdateFcmTokenRequest, ApiResult.EmptyResult>(ApiRoutes.Notifications.UpdateDeviceToken,
                requestA);

        var requestB = new UpdateFcmTokenRequest("devB", "tokenB");
        Factory.ApiClient.SetAuth(authResponseB);
        await Factory.ApiClient
            .PostAsync<UpdateFcmTokenRequest, ApiResult.EmptyResult>(ApiRoutes.Notifications.UpdateDeviceToken,
                requestB);

        // 3. Send invites and await push notifications
        var inviteExpiresAt = Factory.FakeTimeProvider.GetUtcNow() + TimeSpan.FromHours(4);
        await Factory.NotificationManager.AwaitPushNotifications(
            codeToRun: async () =>
            {
                Factory.ApiClient.SetAuth(authResponseA);
                await Factory.ApiClient
                    .PostAsync<InviteUserRequest, ApiResult.EmptyResult>(
                        ApiRoutes.Feed.InviteUser(userBId),
                        new InviteUserRequest(inviteExpiresAt)
                    );

                Factory.ApiClient.SetAuth(authResponseB);
                await Factory.ApiClient
                    .PostAsync<InviteUserRequest, ApiResult.EmptyResult>(
                        ApiRoutes.Feed.InviteUser(userAId),
                        new InviteUserRequest(inviteExpiresAt)
                    );
            },
            expectedCount: 2,
            expectedType: FcmPushBuilder.PushType.MeetingArranged);
    }
}