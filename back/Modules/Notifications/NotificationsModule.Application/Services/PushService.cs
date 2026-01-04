using NotificationModule.Domain.Aggregates;
using NotificationsModule.Infrastructure.Transport.Push;
using Serilog;
using SharedCore.Services.LifeTimes;

namespace NotificationsModule.Application.Services;

public interface IPushService
{
    public Task SendMeetingArrangedPush(Guid userId, string otherUserName, CancellationToken ct = default);
    public Task SendUserInvitedPush(Guid userId, CancellationToken ct = default);
}

internal class FcmPushService : IPushService, IScoped
{
    private readonly IDeviceRepository _deviceRepository;
    private readonly IPushBuilder _pushBuilder;
    private readonly IPushSender _pushSender;

    public FcmPushService(IDeviceRepository deviceRepository, IPushBuilder pushBuilder, IPushSender pushSender)
    {
        _deviceRepository = deviceRepository;
        _pushBuilder = pushBuilder;
        _pushSender = pushSender;
    }

    public async Task SendMeetingArrangedPush(Guid userId, string otherUserName, CancellationToken ct = default)
    {
        await SendPush(
            userId: userId,
            factory: fcmTokens => _pushBuilder.BuildMeetingArrangedPush(fcmTokens, otherUserName),
            ct: ct);
    }

    public async Task SendUserInvitedPush(Guid userId, CancellationToken ct = default)
    {
        await SendPush(
            userId: userId,
            factory: fcmTokens => _pushBuilder.BuildUserInvitedPush(fcmTokens),
            ct: ct);
    }

    public async Task<SendResult> SendMeetingArrangedPush(string[] fcmTokens, string otherUserName)
    {
        var pushMessage = _pushBuilder.BuildMeetingArrangedPush(fcmTokens, otherUserName);
        return await _pushSender.SendPushMessage(pushMessage);
    }

    public async Task<SendResult> SendUserInvitedPush(string[] fcmTokens)
    {
        var pushMessage = _pushBuilder.BuildUserInvitedPush(fcmTokens);
        return await _pushSender.SendPushMessage(pushMessage);
    }

    private async Task SendPush(Guid userId, Func<string[], PushMessage> factory,
        CancellationToken ct = default)
    {
        var fcmTokens = await LoadFcmTokensForUser(userId, ct);
        if (fcmTokens.Length > 0)
        {
            var message = factory.Invoke(fcmTokens);
            var sendResult = await _pushSender.SendPushMessage(message);
            await DeleteExpiredTokens(sendResult.ExpiredTokens, ct);
        }
    }

    private async Task<string[]> LoadFcmTokensForUser(Guid userId, CancellationToken ct = default)
    {
        var devices = await _deviceRepository.GetByUserIdAsync(userId, ct);
        var fcmTokens = devices.Select(d => d.FcmToken).Distinct().ToArray();
        if (fcmTokens.Length == 0)
        {
            Log.Error("User {0} doesn't have any FCM token", userId);
        }

        return fcmTokens;
    }

    private async Task DeleteExpiredTokens(
        string[] expiredTokes,
        CancellationToken ct = default)
    {
        if (expiredTokes.Any())
        {
            foreach (var expiredToken in expiredTokes)
            {
                await _deviceRepository.DeleteByFcmTokenAsync(expiredToken, ct);
            }
        }
    }
}