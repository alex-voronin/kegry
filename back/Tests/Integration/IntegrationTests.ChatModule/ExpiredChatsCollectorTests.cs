using ChatModule.API.Dto;
using ChatModule.Application.BackgroundJobs;
using ChatModule.Infrastructure.Persistence;
using FeedModule.Domain.Services;
using IntegrationTests.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NotificationsModule.API.Dto;
using NotificationsModule.Infrastructure.Transport.Push;
using SharedCore.Api.Dto;
using Shouldly;

namespace IntegrationTests.ChatModule;

[Collection("IntegrationTests")]
public class ExpiredChatsCollectorTests : IntegrationTestBase, IAsyncLifetime
{
    public ExpiredChatsCollectorTests(IntegrationWebApplicationFactory factory) : base(factory)
    {
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public Task DisposeAsync() => ContainerManager.ResetDatabaseAsync();

    [Fact]
    public async Task GivenExpiredChat_WhenCollectingExpiredChats_ThenChatIsDeletedAndLinksAreRemoved()
    {
        Factory.FakeTimeProvider.Reset(
            DateTime.UtcNow - TimeSpan.FromDays(FeedParameters.InviteExpirationTimeInHours + 1)
        );

        // 1. Create two users
        var authA = await Factory.AuthManager.CreateAndLoginUserAsync("testA@example.com", "Password123!");
        Factory.ApiClient.SetAuth(authA);
        await Factory.AuthManager.CreateDefaultProfileAndSubmitLocation();

        var authB = await Factory.AuthManager.CreateAndLoginUserAsync("testB@example.com", "Password123!");
        Factory.ApiClient.SetAuth(authB);
        await Factory.AuthManager.CreateDefaultProfileAndSubmitLocation();

        // 2. Update FCM tokens
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

        // 3. Establish chat
        await Factory.NotificationManager.AwaitPushNotifications(
            codeToRun: async () =>
            {
                Factory.ApiClient.SetAuth(authA);
                await Factory.ApiClient.PostAsync<object?, ApiResult.EmptyResult>(
                    ApiRoutes.Feed.InviteUser(authB.UserId),
                    null);
                Factory.ApiClient.SetAuth(authB);
                await Factory.ApiClient.PostAsync<object?, ApiResult.EmptyResult>(
                    ApiRoutes.Feed.InviteUser(authA.UserId),
                    null);
            },
            expectedCount: 2,
            expectedTitle: NotificationTitles.MeetingArrangedTitle);

        // Verify chat exists
        var chatResponse = await Factory.ApiClient.GetAsync<ApiResult<GetChatResponse>>(ApiRoutes.Chat.ChatRoot);
        chatResponse.Success.ShouldBeTrue();
        var chatId = chatResponse.Result.Id;

        Factory.FakeTimeProvider.Advance(TimeSpan.FromDays(FeedParameters.InviteExpirationTimeInHours + 1));

        // 4. Trigger collection and check that pushes has been sent
        await Factory.NotificationManager.AwaitSilentPushNotifications(
            codeToRun: async () =>
            {
                using var scope = Factory.Services.CreateScope();
                var scopeFactory = scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>();
                var collector = new ExpiredChatsCollectorBackgroundService(scopeFactory);
                await collector.CollectExpiredChats(CancellationToken.None);
            },
            expectedCount: 2,
            expectedType: FcmPushBuilder.PushType.OpenRateUserScreen);

        // 5. Verify that chat has been collected
        using var scope = Factory.Services.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<ChatContext>();
        var chat = await context.Chats.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Id == chatId);
        chat!.IsDeleted.ShouldBeTrue();

        var linksCount = await context.ActiveChatUserLinks.CountAsync(l => l.ChatId == chatId);
        linksCount.ShouldBe(0);
    }
}