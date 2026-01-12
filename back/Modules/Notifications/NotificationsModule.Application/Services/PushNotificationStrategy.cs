using ChatModule.Contracts.Events;
using NotificationModule.Domain.Aggregates;
using NotificationsModule.Infrastructure.Transport.Push;
using Serilog;
using SharedCore.Services.LifeTimes;

namespace NotificationsModule.Application.Services;

public interface IPushNotificationStrategy : INotificationStrategy
{
    public Task SendChatMessageReceivedSilent(ChatMessageSentEvent @event, CancellationToken ct = default);
    public Task SendGoToRateUserScreenSilent(Guid userId, Guid rateeUserId, CancellationToken ct = default);
}

internal class FcmPushNotificationStrategy : IPushNotificationStrategy, IScoped
{
    private readonly IDeviceRepository _deviceRepository;
    private readonly IPushBuilder _pushBuilder;
    private readonly IPushSender _pushSender;

    public FcmPushNotificationStrategy(
        IDeviceRepository deviceRepository,
        IPushBuilder pushBuilder,
        IPushSender pushSender)
    {
        _deviceRepository = deviceRepository;
        _pushBuilder = pushBuilder;
        _pushSender = pushSender;
    }

    public Task SendMeetingArranged(Guid userId, string otherUserFullName, CancellationToken ct = default)
    {
        return SendPush(
            userId: userId,
            factory: fcmTokens => _pushBuilder.BuildMeetingArrangedPush(fcmTokens, otherUserFullName),
            ct: ct);
    }

    public Task SendUserInvited(Guid userId, CancellationToken ct = default)
    {
        return SendPush(
            userId: userId,
            factory: fcmTokens => _pushBuilder.BuildUserInvitedPush(fcmTokens),
            ct: ct);
    }

    public Task SendChatMessageReceived(ChatMessageSentEvent @event, CancellationToken ct = default)
    {
        return SendPush(
            userId: @event.ToUserId,
            factory: fcmTokens => _pushBuilder.BuildChatMessageReceivedPush(fcmTokens, @event),
            ct: ct);
    }

    public Task SendChatMessageReceivedSilent(ChatMessageSentEvent @event, CancellationToken ct = default)
    {
        return SendPush(
            userId: @event.ToUserId,
            factory: fcmTokens => _pushBuilder.BuildChatMessageReceivedSilentPush(fcmTokens, @event),
            ct: ct);
    }

    public Task SendGoToRateUserScreenSilent(Guid userId, Guid rateeUserId, CancellationToken ct = default)
    {
        return SendPush(
            userId: userId,
            factory: fcmTokens => _pushBuilder.BuildGoToRateUserScreenSilentPush(fcmTokens, rateeUserId),
            ct: ct);
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