using FeedModule.Contracts.Events;
using NotificationModule.Domain.Aggregates;
using NotificationsModule.Infrastructure.Transport.Push;
using Serilog;
using Wolverine.Attributes;

namespace NotificationsModule.Application.Messaging;

[WolverineHandler]
public class FeedEventsHandler
{
    public static async Task HandleAsync(
        UsersMutuallyInvited @event,
        IDeviceRepository deviceRepository,
        IPushService pushService,
        CancellationToken ct = default)
    {
        await NotifyMutuallyInvitedUsers(
            userId: @event.FirstUserId,
            otherUserFullName: @event.SecondUserFullName,
            deviceRepository: deviceRepository,
            pushService: pushService,
            ct: ct);

        await NotifyMutuallyInvitedUsers(
            userId: @event.SecondUserId,
            otherUserFullName: @event.FirstUserFullName,
            deviceRepository: deviceRepository,
            pushService: pushService,
            ct: ct);
    }

    private static async Task NotifyMutuallyInvitedUsers(
        Guid userId,
        string otherUserFullName,
        IDeviceRepository deviceRepository,
        IPushService pushService,
        CancellationToken ct = default)
    {
        var fcmTokens = await LoadFcmTokensForUser(userId, deviceRepository, ct);
        if (fcmTokens.Length > 0)
        {
            var sendResult = await pushService.SendInvitationAcceptedPush(fcmTokens, otherUserFullName);
            await DeleteExpiredTokens(sendResult.ExpiredTokens, deviceRepository, ct);
        }
    }

    private static async Task<string[]> LoadFcmTokensForUser(
        Guid userId, 
        IDeviceRepository deviceRepository, 
        CancellationToken ct = default)
    {
        var devices = await deviceRepository.GetByUserIdAsync(userId, ct);
        var fcmTokens = devices.Select(d => d.FcmToken).Distinct().ToArray();
        if (fcmTokens.Length == 0)
        {
            Log.Error("User {0} doesn't have any FCM token", userId);
        }
        return fcmTokens;
    }

    private static async Task DeleteExpiredTokens(
        string[] expiredTokes,
        IDeviceRepository deviceRepository,
        CancellationToken ct = default)
    {
        if (expiredTokes.Any())
        {
            foreach (var expiredToken in expiredTokes)
            {
                await deviceRepository.DeleteByFcmTokenAsync(expiredToken, ct);
            }
        }
    }
}