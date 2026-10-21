using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using NotificationModule.Domain.Aggregates;
using NotificationModule.Domain.Repositories;
using NotificationsModule.API.Dto;
using Shared.Infrastructure.Storage;
using SharedCore.Api;
using SharedCore.Api.Dto;
using SharedCore.Extensions;

namespace NotificationsModule.API;

public class NotificationEndpoints : IEndpoints
{
    public static void MapEndpoints(IEndpointRouteBuilder builder)
    {
        var apiGroup = builder.MapGroup("")
            .WithTags("Notifications API")
            .RequireAuthorization();

        apiGroup.MapPost("/api/v1/notifications/device/token", UpdateFcmToken);
        apiGroup.MapDelete("/api/v1/notifications/device/token", DeleteDevice);
        apiGroup.MapGet("/api/v1/notifications", GetNotifications);
        apiGroup.MapGet("/api/v1/notifications/unseen/count", GetUnseenCount);
        apiGroup.MapGet("/api/v1/notifications/settings", GetSettings);
        apiGroup.MapPut("/api/v1/notifications/settings", UpdateSettings);
        apiGroup.MapDelete("/api/v1/notifications/{id:guid}", DeleteNotification);
    }

    private static async Task<NoContent> UpdateFcmToken(
        [FromBody] UpdateFcmTokenRequest request,
        ClaimsPrincipal user,
        IDeviceRepository deviceRepository,
        CancellationToken ct)
    {
        var device = await deviceRepository.GetByIdAsync(request.DeviceId, ct: ct);

        if (device == null)
        {
            device = new Device
            {
                Id = request.DeviceId,
                FcmToken = request.FcmToken,
                UserId = user.GetId(),
            };
            await deviceRepository.AddAsync(device, ct);
        }
        else
        {
            device.UpdateFcmToken(request.FcmToken);
        }

        await deviceRepository.SaveChangesAsync(ct);

        return TypedResults.NoContent();
    }

    private static async Task<NoContent> DeleteDevice(
        [FromQuery] string deviceId,
        ClaimsPrincipal user,
        IDeviceRepository deviceRepository,
        CancellationToken ct)
    {
        await deviceRepository.DeleteByIdAsync(deviceId, user.GetId(), ct);
        await deviceRepository.SaveChangesAsync(ct);
        return TypedResults.NoContent();
    }

    private static async Task<PaginatedResponse<NotificationDto>> GetNotifications(
        [AsParameters] PaginatedRequest request,
        ClaimsPrincipal user,
        INotificationRepository notificationRepository,
        IS3StorageService storageService,
        CancellationToken ct = default)
    {
        var userId = user.GetId();
        var (notifications, totalCount) = await notificationRepository.GetByUserIdPaginatedAsync(userId,
            request.Offset,
            request.Limit,
            ct);

        if (notifications.Length > 0)
        {
            var idsToMarkAsSeen = notifications.Where(x => !x.IsSeen).Select(x => x.Id).ToArray();
            if (idsToMarkAsSeen.Length > 0)
            {
                await notificationRepository.MarkAsSeenAsync(idsToMarkAsSeen, ct);
            }
        }

        var dtos = notifications.Select(x =>
            new NotificationDto(
                Id: x.Id,
                Type: x.Type,
                FromUserId: x.FromUserId,
                LinkedEntityId: x.LinkedEntityId,
                AvatarImageUrl: x.FromUser != null
                    ? storageService.S3KeyToPublicFileUrl(x.FromUser.AvatarImageS3Key, x.FromUser.UpdatedAt)
                    : null,
                Text: x.Text,
                CreatedAt: x.CreatedAt,
                ExpiresAt: x.ExpiresAt
            )
        ).ToArray();
        return new PaginatedResponse<NotificationDto>(Data: dtos, TotalCount: totalCount);
    }

    private static async Task<int> GetUnseenCount(
        ClaimsPrincipal user,
        INotificationRepository notificationRepository,
        CancellationToken ct = default)
    {
        var count = await notificationRepository.GetUnseenCountAsync(user.GetId(), ct);
        return count;
    }

    private static async Task<GetNotificationSettingsResponse> GetSettings(
        ClaimsPrincipal user,
        INotificationsSettingsRepository settingsRepository,
        CancellationToken ct)
    {
        var userId = user.GetId();
        var settings = await settingsRepository.GetByIdOrDefaultAsync(userId, ct: ct);
        return new GetNotificationSettingsResponse(
            Locale: settings.Locale,
            PushesEnabled: settings.PushesEnabled,
            EmailsEnabled: settings.EmailsEnabled);
    }

    private static async Task<NoContent> UpdateSettings(
        [FromBody] PutNotificationSettingsRequest request,
        ClaimsPrincipal user,
        INotificationsSettingsRepository settingsRepository,
        CancellationToken ct)
    {
        var userId = user.GetId();
        var settings = await settingsRepository.GetByIdAsync(userId, ct: ct);

        if (settings == null)
        {
            settings = new NotificationsSettings
            {
                UserId = userId,
                Locale = request.Locale,
                PushesEnabled = request.PushesEnabled,
                EmailsEnabled = request.EmailsEnabled
            };
            await settingsRepository.AddAsync(settings, ct);
        }
        else
        {
            settings.Locale = request.Locale;
            settings.PushesEnabled = request.PushesEnabled;
            settings.EmailsEnabled = request.EmailsEnabled;
        }

        await settingsRepository.SaveChangesAsync(ct);
        return TypedResults.NoContent();
    }

    private static async Task<NoContent> DeleteNotification(
        [FromRoute] Guid id,
        ClaimsPrincipal user,
        INotificationRepository notificationRepository,
        CancellationToken ct)
    {
        var notification = await notificationRepository.GetByIdAsync(id, ct: ct);
        if (notification == null || notification.UserId != user.GetId())
        {
            return TypedResults.NoContent();
        }

        notificationRepository.Delete(notification);
        await notificationRepository.SaveChangesAsync(ct);
        return TypedResults.NoContent();
    }
}