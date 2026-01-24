using IntegrationTests.Shared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using NotificationModule.Domain.Aggregates;
using NotificationsModule.API.Dto;
using NotificationsModule.Infrastructure.Persistence;
using SharedCore.Api.Dto;
using Shouldly;

namespace IntegrationTests.NotificationsModule;

[Collection("IntegrationTests")]
public class NotificationEndpointsTests : IntegrationTestBase, IAsyncLifetime
{
    public NotificationEndpointsTests(IntegrationWebApplicationFactory factory) : base(factory)
    {
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public Task DisposeAsync() => ContainerManager.ResetDatabaseAsync();

    [Fact]
    public async Task GivenAuthorizedUserWithTwoPagesOfNotifications_WhenUserGetsTheSecondPage_ThenReceivesTheSecondPage()
    {
        // 1. Authorize
        var authResponse = await Factory.AuthManager.CreateAndLoginUserAsync("user@example.com", "Password123!");
        var userId = Guid.Parse(authResponse.UserId);
        Factory.ApiClient.SetAuth(authResponse);

        // 2. Create the profile and notification user as well
        await Factory.AuthManager.CreateDefaultProfileAndSubmitLocation();

        // 3. Save notifications to the database
        using (var scope = Factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<NotificationsContext>();
            for (int i = 0; i < 20; i++)
            {
                context.Notifications.Add(new Notification
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-i),
                    PrimaryText = $"Notification {i}",
                    SecondaryText = $"Description {i}",
                    FromUserId = userId,
                    IsSeen = false
                });
            }

            await context.SaveChangesAsync();
        }

        // 4. Get the notifications page
        var response = await Factory.ApiClient.GetAsync<ApiResult<PaginatedResponse<NotificationDto>>>(
            ApiRoutes.Notifications.GetNotificationsPaginated(10, 10));

        // 5. Check the response
        response.Success.ShouldBeTrue();
        var data = response.Result.Data.ToList();
        data.Count.ShouldBe(10);
        response.Result.TotalCount.ShouldBe(20);
        data[0].PrimaryText.ShouldBe("Notification 10");
        data[0].SecondaryText.ShouldBe("Description 10");
        data[0].AvatarImageUrl.ShouldBe(TestConfig.S3.PublicUrl + "/avatar.jpg");
    }

    [Fact]
    public async Task GivenAuthorizedUserUnseenNotifications_WhenUserGetsUnseenNotifications_ThenTheirCountDecreases()
    {
        // 1. Authorize
        var authResponse = await Factory.AuthManager.CreateAndLoginUserAsync("user@example.com", "Password123!");
        var userId = Guid.Parse(authResponse.UserId);
        Factory.ApiClient.SetAuth(authResponse);

        // 2. Create the profile and notification user as well
        await Factory.AuthManager.CreateDefaultProfileAndSubmitLocation();

        // 3. Create an unseen notification
        using (var scope = Factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<NotificationsContext>();

            context.Notifications.Add(new Notification
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                CreatedAt = DateTimeOffset.UtcNow,
                PrimaryText = "Unseen Notification",
                SecondaryText = "Description",
                IsSeen = false
            });
            await context.SaveChangesAsync();
        }

        // 4. Get notifications
        var response = await Factory.ApiClient.GetAsync<ApiResult<PaginatedResponse<NotificationDto>>>(
            ApiRoutes.Notifications.GetNotificationsPaginated(0, 10));

        // 3. Check that the notification is marked as seen
        response.Success.ShouldBeTrue();
        using (var scope = Factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<NotificationsContext>();
            var notification = await context.Notifications.FirstAsync(x => x.UserId == userId);
            notification.IsSeen.ShouldBeTrue();
        }
    }

    [Fact]
    public async Task GivenFewUnseenNotifications_WhenUserGetsUnseenNotificationsCount_ThenReceivesCorrectCount()
    {
        // 1. Authorize
        var authResponse = await Factory.AuthManager.CreateAndLoginUserAsync("user@example.com", "Password123!");
        var userId = Guid.Parse(authResponse.UserId);
        Factory.ApiClient.SetAuth(authResponse);

        // 2. Create the profile and notification user as well
        await Factory.AuthManager.CreateDefaultProfileAndSubmitLocation();

        using (var scope = Factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<NotificationsContext>();
            context.Notifications.Add(new Notification
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                CreatedAt = DateTimeOffset.UtcNow,
                PrimaryText = "Unseen 1",
                SecondaryText = "Description",
                IsSeen = false
            });
            context.Notifications.Add(new Notification
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                CreatedAt = DateTimeOffset.UtcNow,
                PrimaryText = "Seen 1",
                SecondaryText = "Description",
                IsSeen = true
            });
            context.Notifications.Add(new Notification
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                CreatedAt = DateTimeOffset.UtcNow,
                PrimaryText = "Unseen 2",
                SecondaryText = "Description",
                IsSeen = false
            });
            await context.SaveChangesAsync();
        }

        // 3. Get unseen count
        var response = await Factory.ApiClient.GetAsync<ApiResult<int>>(ApiRoutes.Notifications.GetUnseenCount);

        // 4. Check response
        response.Success.ShouldBeTrue();
        response.Result.ShouldBe(2);
    }

    [Fact]
    public async Task GivenAuthorizedUser_WhenUserUpdatesSettings_ThenSettingsAreUpdated()
    {
        // 1. Authorize and create user profile
        var authResponse = await Factory.AuthManager.CreateAndLoginUserAsync("user@example.com", "Password123!");
        Factory.ApiClient.SetAuth(authResponse);
        await Factory.AuthManager.CreateDefaultProfileAndSubmitLocation();

        // 2. Get initial settings (should be default)
        var initialResponse = await Factory.ApiClient.GetAsync<ApiResult<GetNotificationSettingsResponse>>(
                ApiRoutes.Notifications
                .Settings);
        initialResponse.Success.ShouldBeTrue();
        initialResponse.Result.PushesEnabled.ShouldBeTrue();
        initialResponse.Result.EmailsEnabled.ShouldBeFalse();

        // 3. Update settings to true
        var enableAllRequest = new PutNotificationSettingsRequest(PushesEnabled: true, EmailsEnabled: true);
        var enableAllResponse =
            await Factory.ApiClient.PutAsync<PutNotificationSettingsRequest, ApiResult.EmptyResult>(
                ApiRoutes.Notifications.Settings, enableAllRequest);
        enableAllResponse.Success.ShouldBeTrue();

        var allEnabledResponse = await Factory.ApiClient
            .GetAsync<ApiResult<GetNotificationSettingsResponse>>(ApiRoutes.Notifications.Settings);
        allEnabledResponse.Success.ShouldBeTrue();
        allEnabledResponse.Result.PushesEnabled.ShouldBeTrue();
        allEnabledResponse.Result.EmailsEnabled.ShouldBeTrue();

        // 4. Update again to false
        var disableAllRequest = new PutNotificationSettingsRequest(PushesEnabled: false, EmailsEnabled: false);
        await Factory.ApiClient.PutAsync<PutNotificationSettingsRequest, ApiResult.EmptyResult>(
            ApiRoutes.Notifications.Settings, disableAllRequest);

        var allDisabledResponse = await Factory.ApiClient
                .GetAsync<ApiResult<GetNotificationSettingsResponse>>(ApiRoutes.Notifications.Settings);
        allDisabledResponse.Result.PushesEnabled.ShouldBeFalse();
        allDisabledResponse.Result.EmailsEnabled.ShouldBeFalse();
    }
}