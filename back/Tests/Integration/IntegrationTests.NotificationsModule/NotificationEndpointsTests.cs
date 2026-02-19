using IntegrationTests.Shared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using NotificationModule.Domain.Aggregates;
using NotificationModule.Domain.ValueObjects;
using NotificationsModule.API.Dto;
using NotificationsModule.Infrastructure.Persistence;
using SharedCore.Api.Dto;
using Shouldly;
using Wolverine.Tracking;

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
    public async Task GivenUserWithTwoPagesOfNotifications_WhenUserGetsTheSecondPage_ThenReceivesTheSecondPage()
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
                var now = DateTimeOffset.UtcNow;
                context.Notifications.Add(new Notification
                {
                    Id = Guid.NewGuid(),
                    Type = NotificationType.Invite,
                    UserId = userId,
                    CreatedAt = now.AddMinutes(-i),
                    ExpiresAt = now,
                    Text = $"Notification {i}",
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
        data[0].Text.ShouldBe("Notification 10");
        data[0].Type.ShouldBe(NotificationType.Invite);
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
                Type = NotificationType.Invite,
                UserId = userId,
                CreatedAt = DateTimeOffset.UtcNow,
                ExpiresAt = DateTimeOffset.UtcNow,
                Text = "Unseen Notification",
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
                Type = NotificationType.Invite,
                UserId = userId,
                CreatedAt = DateTimeOffset.UtcNow,
                ExpiresAt = DateTimeOffset.UtcNow,
                Text = "Unseen 1",
                IsSeen = false
            });
            context.Notifications.Add(new Notification
            {
                Id = Guid.NewGuid(),
                Type = NotificationType.Invite,
                UserId = userId,
                CreatedAt = DateTimeOffset.UtcNow,
                ExpiresAt = DateTimeOffset.UtcNow,
                Text = "Seen 1",
                IsSeen = true
            });
            context.Notifications.Add(new Notification
            {
                Id = Guid.NewGuid(),
                Type = NotificationType.Invite,
                UserId = userId,
                CreatedAt = DateTimeOffset.UtcNow,
                ExpiresAt = DateTimeOffset.UtcNow,
                Text = "Unseen 2",
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
        var initialResponse = await Factory.ApiClient
            .GetAsync<ApiResult<GetNotificationSettingsResponse>>(ApiRoutes.Notifications.Settings);
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

    [Fact]
    public async Task GivenAuthorizedUser_WhenUserUpdatesFCMToken_ThenTokenIsSaved()
    {
        // 1. Authorize
        var authResponse = await Factory.AuthManager.CreateAndLoginUserAsync("user-token@example.com", "Password123!");
        var userId = Guid.Parse(authResponse.UserId);
        Factory.ApiClient.SetAuth(authResponse);
        await Factory.AuthManager.CreateDefaultProfileAndSubmitLocation();

        // 2. Update FCM Token
        var deviceId = "device-123";
        var fcmToken = "fcm-token-123";
        var request = new UpdateFcmTokenRequest(deviceId, fcmToken);

        var response = await Factory.ApiClient.PostAsync<UpdateFcmTokenRequest, ApiResult.EmptyResult>(
            ApiRoutes.Notifications.UpdateDeviceToken, request);

        // 3. Verify response and database
        response.Success.ShouldBeTrue();

        using (var scope = Factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<NotificationsContext>();
            var device = await context.Devices.FirstOrDefaultAsync(d => d.Id == deviceId);

            device.ShouldNotBeNull();
            device.FcmToken.ShouldBe(fcmToken);
            device.UserId.ShouldBe(userId);
        }
    }

    [Fact]
    public async Task GivenAuthorizedUserWithExistingDevice_WhenUserUpdatesFCMToken_ThenTokenIsUpdated()
    {
        // 1. Authorize
        var authResponse =
            await Factory.AuthManager.CreateAndLoginUserAsync("user-update-token@example.com", "Password123!");
        var userId = Guid.Parse(authResponse.UserId);
        Factory.ApiClient.SetAuth(authResponse);
        await Factory.AuthManager.CreateDefaultProfileAndSubmitLocation();

        // 2. Create existing device
        var deviceId = "device-abc";
        using (var scope = Factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<NotificationsContext>();
            context.Devices.Add(
                new Device()
                {
                    Id = deviceId,
                    FcmToken = "old-fcm-token",
                    UserId = userId
                }
            );
            await context.SaveChangesAsync();
        }

        // 3. Update FCM Token
        var newFcmToken = "new-fcm-token";
        var request = new UpdateFcmTokenRequest(deviceId, newFcmToken);

        var response = await Factory.ApiClient.PostAsync<UpdateFcmTokenRequest, ApiResult.EmptyResult>(
            ApiRoutes.Notifications.UpdateDeviceToken, request);

        // 4. Verify update
        response.Success.ShouldBeTrue();

        using (var scope = Factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<NotificationsContext>();
            var device = await context.Devices.FirstOrDefaultAsync(d => d.Id == deviceId);

            device.ShouldNotBeNull();
            device.FcmToken.ShouldBe(newFcmToken);
        }
    }

    [Fact]
    public async Task GivenAuthorizedUserWithExistingDevice_WhenUserDeletesDevice_ThenDeviceIsRemoved()
    {
        // 1. Authorize
        var authResponse =
            await Factory.AuthManager.CreateAndLoginUserAsync("user-delete-device@example.com", "Password123!");
        var userId = Guid.Parse(authResponse.UserId);
        Factory.ApiClient.SetAuth(authResponse);
        await Factory.AuthManager.CreateDefaultProfileAndSubmitLocation();

        // 2. Create existing device
        var deviceId = "device-to-delete";
        using (var scope = Factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<NotificationsContext>();
            context.Devices.Add(
                new Device()
                {
                    Id = deviceId,
                    FcmToken = "fcm-token-456",
                    UserId = userId
                }
            );
            await context.SaveChangesAsync();
        }

        // 3. Delete device
        var response = await Factory.ApiClient.DeleteAsync<ApiResult.EmptyResult>(
            ApiRoutes.Notifications.DeleteDeviceToken(deviceId));

        // 4. Verify removal
        response.Success.ShouldBeTrue();

        using (var scope = Factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<NotificationsContext>();
            var device = await context.Devices.FirstOrDefaultAsync(d => d.Id == deviceId);

            device.ShouldBeNull();
        }
    }

    [Fact]
    public async Task GivenUserBInvitesUserA_WhenUserADeletesNotification_ThenNotificationListIsEmpty()
    {
        // 1. Create and authorize User A
        var authA = await Factory.AuthManager.CreateAndLoginUserAsync("user-a-delete@example.com", "Password123!");
        var userAId = authA.UserId;
        Factory.ApiClient.SetAuth(authA);
        await Factory.AuthManager.CreateDefaultProfileAndSubmitLocation();

        // 2. Create and authorize User B
        var authB = await Factory.AuthManager.CreateAndLoginUserAsync("user-b-delete@example.com", "Password123!");
        var userBId = authB.UserId;
        Factory.ApiClient.SetAuth(authB);
        await Factory.AuthManager.CreateDefaultProfileAndSubmitLocation();

        // 3. User B invites User A (triggers UserInvitedEvent → creates notification for User A)
        await Factory.Host.ExecuteAndWaitAsync(async () =>
        {
            var inviteResponse = await Factory.ApiClient.PostAsync<object, ApiResult.EmptyResult>(
                ApiRoutes.Feed.InviteUser(userAId), new { });
            inviteResponse.Success.ShouldBeTrue();
        });

        // 4. Switch to User A and fetch notifications
        Factory.ApiClient.SetAuth(authA);
        var notificationsResponse = await Factory.ApiClient.GetAsync<ApiResult<PaginatedResponse<NotificationDto>>>(
            ApiRoutes.Notifications.GetNotificationsPaginated(0, 10));
        notificationsResponse.Success.ShouldBeTrue();
        notificationsResponse.Result.Data.Count.ShouldBe(1);

        var notification = notificationsResponse.Result.Data.First();
        notification.FromUserId.ShouldBe(Guid.Parse(userBId));

        // 5. User A deletes the notification
        var deleteResponse = await Factory.ApiClient.DeleteAsync<ApiResult.EmptyResult>(
            ApiRoutes.Notifications.DeleteNotification(notification.Id));
        deleteResponse.Success.ShouldBeTrue();

        // 6. Verify notification list is empty
        var afterDeleteResponse = await Factory.ApiClient.GetAsync<ApiResult<PaginatedResponse<NotificationDto>>>(
            ApiRoutes.Notifications.GetNotificationsPaginated(0, 10));
        afterDeleteResponse.Success.ShouldBeTrue();
        afterDeleteResponse.Result.Data.Count.ShouldBe(0);
        afterDeleteResponse.Result.TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task GivenNotification_WhenAnotherUserTriesToDeleteIt_ThenNotificationIsNotRemoved()
    {
        // 1. Create User A with a notification
        var authA = await Factory.AuthManager.CreateAndLoginUserAsync("owner@example.com", "Password123!");
        var userAId = Guid.Parse(authA.UserId);
        Factory.ApiClient.SetAuth(authA);
        await Factory.AuthManager.CreateDefaultProfileAndSubmitLocation();

        var notificationId = Guid.NewGuid();
        using (var scope = Factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<NotificationsContext>();
            context.Notifications.Add(new Notification
            {
                Id = notificationId,
                Type = NotificationType.Invite,
                UserId = userAId,
                CreatedAt = DateTimeOffset.UtcNow,
                ExpiresAt = DateTimeOffset.UtcNow.AddHours(24),
                Text = "User A's notification",
                IsSeen = false
            });
            await context.SaveChangesAsync();
        }

        // 2. Create User B
        var authB = await Factory.AuthManager.CreateAndLoginUserAsync("intruder@example.com", "Password123!");
        Factory.ApiClient.SetAuth(authB);
        await Factory.AuthManager.CreateDefaultProfileAndSubmitLocation();

        // 3. User B tries to delete User A's notification
        var response = await Factory.ApiClient.DeleteAsync<ApiResult.EmptyResult>(
            ApiRoutes.Notifications.DeleteNotification(notificationId));
        response.Success.ShouldBeTrue(); // Returns success but doesn't actually delete

        // 4. Switch to User A and fetch notifications
        Factory.ApiClient.SetAuth(authA);
        var notificationsResponse = await Factory.ApiClient.GetAsync<ApiResult<PaginatedResponse<NotificationDto>>>(
            ApiRoutes.Notifications.GetNotificationsPaginated(0, 10));
        notificationsResponse.Success.ShouldBeTrue();
        notificationsResponse.Result.Data.Count.ShouldBe(1);
        notificationsResponse.Result.Data.First().Id.ShouldBe(notificationId);
    }
}