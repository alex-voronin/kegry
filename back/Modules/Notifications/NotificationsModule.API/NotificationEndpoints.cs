using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using NotificationModule.Domain.Aggregates;
using NotificationModule.Domain.Repositories;
using NotificationsModule.API.Dto;
using SharedCore.Api;
using SharedCore.Api.Dto;
using SharedCore.Extensions;

namespace NotificationsModule.API;

public record UpdateFcmTokenRequest(string DeviceId, string FcmToken);

public class NotificationEndpoints : IEndpoints
{
    public static void MapEndpoints(IEndpointRouteBuilder builder)
    {
        var apiGroup = builder.MapGroup("")
            .WithTags("Notifications API")
            .AllowAnonymous();

        apiGroup.MapPost("/api/v1/notifications/device/token", UpdateFcmToken);
        apiGroup.MapGet("/api/v1/notifications", GetNotifications);
        apiGroup.MapGet("/api/v1/notifications/unseen/count", GetUnseenCount);
    }

    private static async Task UpdateFcmToken(
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
            deviceRepository.Update(device);
        }

        await deviceRepository.SaveChangesAsync(ct);
    }

    private static async Task<ApiResult<PaginatedResponse<NotificationDto>>> GetNotifications(
        [AsParameters] PaginatedRequest request,
        ClaimsPrincipal user,
        INotificationRepository notificationRepository,
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
                FromUserId: x.FromUserId,
                AvatarImageUrl: x.FromUser?.AvatarImageUrl,
                PrimaryText: x.PrimaryText,
                SecondaryText: x.SecondaryText,
                CreatedAt: x.CreatedAt
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
}