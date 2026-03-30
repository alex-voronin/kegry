using FeedModule.API.Dto;
using IntegrationTests.Shared;
using Microsoft.Extensions.Time.Testing;
using NotificationModule.Domain.ValueObjects;
using NotificationsModule.API.Dto;
using NotificationsModule.Infrastructure.Transport.Push;
using SharedCore.Api.Dto;
using Shouldly;

namespace IntegrationTests.NotificationsModule;

[Collection("IntegrationTests")]
public class PushesLocalizationTests : IntegrationTestBase, IAsyncLifetime
{
    public PushesLocalizationTests(IntegrationWebApplicationFactory factory) : base(factory)
    {
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public Task DisposeAsync() => ContainerManager.ResetDatabaseAsync();

    [Fact]
    public async Task GivenTwoUsers_WhenTheyCreateChat_ThenTheyBothReceiveNotification()
    {
        // 1. Create users
        var authResponseA = await Factory.AuthManager.CreateAndLoginUserAsync("sr@example.com", "Password123!");
        var userAId = authResponseA.UserId;
        Factory.ApiClient.SetAuth(authResponseA);
        await Factory.AuthManager.CreateDefaultProfileAndSubmitLocation(locale: SupportedLocales.Serbian);

        var authResponseB = await Factory.AuthManager.CreateAndLoginUserAsync("enB@example.com", "Password123!");
        var userBId = authResponseB.UserId;
        Factory.ApiClient.SetAuth(authResponseB);
        await Factory.AuthManager.CreateDefaultProfileAndSubmitLocation(locale: SupportedLocales.English);

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

        // 3. Send invite from A(sr) to B(en)
        var inviteExpirationHours = 4;
        var inviteExpiresAt = Factory.FakeTimeProvider.GetUtcNow() + TimeSpan.FromHours(inviteExpirationHours);
        await Factory.NotificationManager.AwaitPushNotifications(
            codeToRun: async () =>
            {
                Factory.ApiClient.SetAuth(authResponseA);
                var inviteResponseA = await Factory.ApiClient
                    .PostAsync<InviteUserRequest, ApiResult.EmptyResult>(
                        ApiRoutes.Feed.InviteUser(userBId),
                        new InviteUserRequest(inviteExpiresAt)
                    );
                inviteResponseA.Success.ShouldBeTrue();
            },
            expectedCount: 1,
            containedTextInTitle: "invitation", // en as A(sr) invites B(en)
            expectedType: FcmPushBuilder.PushType.UserInvited);
        
        // 4. Check that stored notification has a correct locale
        Factory.ApiClient.SetAuth(authResponseB);
        var notificationsResponseB = await Factory.ApiClient.GetAsync<ApiResult<PaginatedResponse<NotificationDto>>>(
            ApiRoutes.Notifications.GetNotificationsPaginated(0, 10));
        notificationsResponseB.Result.Data.Count.ShouldBe(1);
        notificationsResponseB.Result.Data.First().Text.ShouldContain("drink");
        
        // 5. Send invite from B(en) to A(sr) 
        Factory.FakeTimeProvider.Advance(TimeSpan.FromHours(inviteExpirationHours + 1)); 
        await Factory.NotificationManager.AwaitPushNotifications(
            codeToRun: async () =>
            {
                Factory.ApiClient.SetAuth(authResponseB);
                var inviteResponseB = await Factory.ApiClient
                    .PostAsync<InviteUserRequest, ApiResult.EmptyResult>(
                        ApiRoutes.Feed.InviteUser(userAId),
                        new InviteUserRequest(inviteExpiresAt)
                    );
                inviteResponseB.Success.ShouldBeTrue();
            },
            expectedCount: 1,
            containedTextInTitle: "pozivnica", // sr as B(en) invites A(sr) 
            expectedType: FcmPushBuilder.PushType.UserInvited);
        
        // 6. Check that stored notification has a correct locale
        Factory.ApiClient.SetAuth(authResponseA);
        var notificationsResponseA = await Factory.ApiClient.GetAsync<ApiResult<PaginatedResponse<NotificationDto>>>(
            ApiRoutes.Notifications.GetNotificationsPaginated(0, 10));
        notificationsResponseA.Result.Data.Count.ShouldBe(1);
        notificationsResponseA.Result.Data.First().Text.ShouldContain("piće");
    }
}