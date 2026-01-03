using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using NotificationModule.Domain.Aggregates;
using SharedCore.Api;
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
}