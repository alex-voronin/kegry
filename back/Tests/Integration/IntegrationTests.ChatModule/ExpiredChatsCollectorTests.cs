using ChatModule.API.Dto;
using ChatModule.Application.BackgroundJobs;
using ChatModule.Infrastructure.Persistence;
using FeedModule.Domain.Services;
using IntegrationTests.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NotificationsModule.API;
using NotificationsModule.Infrastructure.Transport.Push;
using SharedCore.Api.Dto;
using Shouldly;

namespace IntegrationTests.ChatModule;

[Collection("IntegrationTests")]
public class ExpiredChatsCollectorTests : IAsyncLifetime
{
    private readonly IntegrationWebApplicationFactory _factory;

    public ExpiredChatsCollectorTests(IntegrationWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public Task DisposeAsync() => _factory.ResetDatabaseAsync();

    [Fact]
    public async Task GivenExpiredChat_WhenCollectingExpiredChats_ThenChatIsDeletedAndLinksAreRemoved()
    {
        _factory.FakeTimeProvider.SetUtcNow(
            DateTime.UtcNow - TimeSpan.FromDays(FeedParameters.MeetingExpirationTimeInDays + 1)
        );

        // 1. Create two users
        var authA = await _factory.AuthManager.CreateAndLoginUserAsync("testA@example.com", "Password123!");
        _factory.ApiClient.SetAuth(authA);
        await _factory.AuthManager.CreateDefaultProfileAndSubmitLocation();

        var authB = await _factory.AuthManager.CreateAndLoginUserAsync("testB@example.com", "Password123!");
        _factory.ApiClient.SetAuth(authB);
        await _factory.AuthManager.CreateDefaultProfileAndSubmitLocation();

        // 2. Update FCM tokens
        var tokenRequestA = new UpdateFcmTokenRequest("devA", "tokenA");
        _factory.ApiClient.SetAuth(authA);
        await _factory.ApiClient
            .PostAsync<UpdateFcmTokenRequest, ApiResult.EmptyResult>(ApiRoutes.Notifications.UpdateDeviceToken,
                tokenRequestA);

        var tokenRequestB = new UpdateFcmTokenRequest("devB", "tokenB");
        _factory.ApiClient.SetAuth(authB);
        await _factory.ApiClient
            .PostAsync<UpdateFcmTokenRequest, ApiResult.EmptyResult>(ApiRoutes.Notifications.UpdateDeviceToken,
                tokenRequestB);

        // 3. Establish chat
        _factory.ApiClient.SetAuth(authA);
        await _factory.ApiClient.PostAsync<object?, ApiResult.EmptyResult>(ApiRoutes.Feed.InviteUser(authB.UserId),
            null);
        _factory.ApiClient.SetAuth(authB);
        await _factory.ApiClient.PostAsync<object?, ApiResult.EmptyResult>(ApiRoutes.Feed.InviteUser(authA.UserId),
            null);

        // Verify chat exists
        var chatResponse = await _factory.ApiClient.GetAsync<ApiResult<GetChatResponse>>(ApiRoutes.Chat.ChatRoot);
        chatResponse.Success.ShouldBeTrue();
        var chatId = chatResponse.Result.Id;

        _factory.FakeTimeProvider.Advance(TimeSpan.FromDays(FeedParameters.MeetingExpirationTimeInDays + 1));

        // 4. Trigger collection and check that pushes has been sent
        await _factory.NotificationManager.AwaitSilentPushNotifications(
            codeToRun: async () =>
            {
                using var scope = _factory.Services.CreateScope();
                var scopeFactory = scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>();
                var collector = new ExpiredChatsCollectorBackgroundService(scopeFactory);
                await collector.CollectExpiredChats(CancellationToken.None);
            },
            expectedCount: 2,
            expectedAction: FcmPushBuilder.Actions.OpenRateUserScreen);

        // 5. Verify that chat has been collected
        using var scope = _factory.Services.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<ChatContext>();
        var chat = await context.Chats.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Id == chatId);
        chat!.IsDeleted.ShouldBeTrue();

        var linksCount = await context.ActiveChatUserLinks.CountAsync(l => l.ChatId == chatId);
        linksCount.ShouldBe(0);
    }
}