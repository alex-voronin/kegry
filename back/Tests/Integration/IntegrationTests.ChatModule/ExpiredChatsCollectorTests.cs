using ChatModule.API.Dto;
using ChatModule.Application.BackgroundJobs;
using ChatModule.Infrastructure.Persistence;
using FeedModule.API.Dto;
using IntegrationTests.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NotificationsModule.API.Dto;
using NotificationsModule.Infrastructure.Transport.Push;
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
        var expirationSpan = TimeSpan.FromHours(4);
        var expiresAt = DateTimeOffset.UtcNow - expirationSpan;

        Factory.FakeTimeProvider.Reset(
            expiresAt - TimeSpan.FromHours(1)
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
            .PostAsync<UpdateFcmTokenRequest>(ApiRoutes.Notifications.UpdateDeviceToken,
                tokenRequestA);

        var tokenRequestB = new UpdateFcmTokenRequest("devB", "tokenB");
        Factory.ApiClient.SetAuth(authB);
        await Factory.ApiClient
            .PostAsync<UpdateFcmTokenRequest>(ApiRoutes.Notifications.UpdateDeviceToken,
                tokenRequestB);

        // 3. Establish chat
        await Factory.NotificationManager.AwaitPushNotifications(
            codeToRun: async () =>
            {
                Factory.ApiClient.SetAuth(authA);
                await Factory.ApiClient.PostAsync<InviteUserRequest>(
                    ApiRoutes.Feed.InviteUser(authB.UserId),
                    new InviteUserRequest(expiresAt)
                );

                Factory.ApiClient.SetAuth(authB);
                await Factory.ApiClient.PostAsync<InviteUserRequest>(
                    ApiRoutes.Feed.InviteUser(authA.UserId),
                    new InviteUserRequest(expiresAt)
                );
            },
            expectedCount: 2,
            expectedType: FcmPushBuilder.PushType.MeetingArranged);

        // Verify chat exists
        var chatResponse = await Factory.ApiClient.GetAsync<GetChatResponse>(ApiRoutes.Chat.ChatRoot);
        var chatId = chatResponse.Id;

        Factory.FakeTimeProvider.Advance(expirationSpan + TimeSpan.FromHours(1));

        // 4. Trigger collection and check that pushes has been sent
        await Factory.NotificationManager.AwaitPushNotifications(
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