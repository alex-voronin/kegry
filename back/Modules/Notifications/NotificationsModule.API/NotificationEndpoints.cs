using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
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
            .AllowAnonymous();

        apiGroup.MapPost("/api/v1/notifications/device/token", UpdateFcmToken);
        apiGroup.MapDelete("/api/v1/notifications/device/token", DeleteDevice);
        apiGroup.MapGet("/api/v1/notifications", GetNotifications);
        apiGroup.MapGet("/api/v1/notifications/unseen/count", GetUnseenCount);
        apiGroup.MapGet("/api/v1/notifications/settings", GetSettings);
        apiGroup.MapPut("/api/v1/notifications/settings", UpdateSettings);
        apiGroup.MapDelete("/api/v1/notifications/{id:guid}", DeleteNotification);
    }

    private static async Task<ApiResult.EmptyResult> UpdateFcmToken(
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

        return ApiResult.Empty;
    }

    private static async Task<ApiResult.EmptyResult> DeleteDevice(
        [FromQuery] string deviceId,
        ClaimsPrincipal user,
        IDeviceRepository deviceRepository,
        CancellationToken ct)
    {
        await deviceRepository.DeleteByIdAsync(deviceId, user.GetId(), ct);
        await deviceRepository.SaveChangesAsync(ct);
        return ApiResult.Empty;
    }

    private static async Task<ApiResult<PaginatedResponse<NotificationDto>>> GetNotifications(
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
                AvatarImageUrl: x.FromUser != null
                    ? storageService.S3KeyToPublicFileUrl(x.FromUser.AvatarImageS3Key)
                    : null,
                Text: x.Text,
                CreatedAt: x.CreatedAt,
                ExpiresAt: x.ExpiresAt
            )
        ).ToArray();
        return ApiResult<PaginatedResponse<NotificationDto>>.Build(
            new PaginatedResponse<NotificationDto>(Data: dtos, TotalCount: totalCount)
        );
    }

    private static async Task<ApiResult<int>> GetUnseenCount(
        ClaimsPrincipal user,
        INotificationRepository notificationRepository,
        CancellationToken ct = default)
    {
        var count = await notificationRepository.GetUnseenCountAsync(user.GetId(), ct);
        return ApiResult<int>.Build(count);
    }

    private static async Task<ApiResult<GetNotificationSettingsResponse>> GetSettings(
        ClaimsPrincipal user,
        INotificationsSettingsRepository settingsRepository,
        CancellationToken ct)
    {
        var userId = user.GetId();
        var (pushes, emails) = await settingsRepository.GetNotificationsEnabledById(userId, ct);
        return ApiResult<GetNotificationSettingsResponse>.Build(new GetNotificationSettingsResponse(pushes, emails));
    }

    private static async Task<ApiResult.EmptyResult> UpdateSettings(
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
                PushesEnabled = request.PushesEnabled,
                EmailsEnabled = request.EmailsEnabled
            };
            await settingsRepository.AddAsync(settings, ct);
        }
        else
        {
            settings.PushesEnabled = request.PushesEnabled;
            settings.EmailsEnabled = request.EmailsEnabled;
        }

        await settingsRepository.SaveChangesAsync(ct);
        return ApiResult.Empty;
    }

    private static async Task<ApiResult.EmptyResult> DeleteNotification(
        [FromRoute] Guid id,
        ClaimsPrincipal user,
        INotificationRepository notificationRepository,
        CancellationToken ct)
    {
        var notification = await notificationRepository.GetByIdAsync(id, ct: ct);
        if (notification == null || notification.UserId != user.GetId())
        {
            return ApiResult.Empty;
        }

        notificationRepository.Delete(notification);
        await notificationRepository.SaveChangesAsync(ct);
        return ApiResult.Empty;
    }
}