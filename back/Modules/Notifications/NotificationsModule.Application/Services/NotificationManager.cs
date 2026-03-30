using System.Globalization;
using ChatModule.Contracts.Events;
using NotificationModule.Domain.Repositories;
using SharedCore.Services.LifeTimes;

namespace NotificationsModule.Application.Services;

public interface INotificationManager : INotificationStrategy;

internal class NotificationManager : INotificationManager, IScoped
{
    private readonly INotificationsSettingsRepository _notificationsSettingsRepository;
    private readonly IEmailNotificationStrategy _emailNotificationStrategy;
    private readonly IPushNotificationStrategy _pushNotificationStrategy;

    public NotificationManager(
        INotificationsSettingsRepository notificationsSettingsRepository,
        IEmailNotificationStrategy emailNotificationStrategy,
        IPushNotificationStrategy pushNotificationStrategy)
    {
        _notificationsSettingsRepository = notificationsSettingsRepository;
        _emailNotificationStrategy = emailNotificationStrategy;
        _pushNotificationStrategy = pushNotificationStrategy;
    }

    public Task SendMeetingArranged(Guid userId, string otherUserFullName, CancellationToken ct = default)
    {
        return SendNotifications(userId,
            sendFunc: strategy => strategy.SendMeetingArranged(userId, otherUserFullName, ct),
            ct: ct
        );
    }

    public Task SendUserInvited(Guid userId, CancellationToken ct = default)
    {
        return SendNotifications(userId,
            sendFunc: strategy => strategy.SendUserInvited(userId, ct),
            ct: ct
        );
    }

    public Task SendChatMessageReceived(ChatMessageSentEvent @event, CancellationToken ct = default)
    {
        return SendNotifications(@event.ToUserId,
                sendFunc: strategy => strategy.SendChatMessageReceived(@event, ct),
                ct: ct
            );
    }

    public Task SendReferrerRewardReceived(Guid userId, CancellationToken ct = default)
    {
        return SendNotifications(userId,
            sendFunc: strategy => strategy.SendReferrerRewardReceived(userId, ct),
            ct: ct
        );
    }

    private async Task SendNotifications(
        Guid userId,
        Func<INotificationStrategy, Task> sendFunc,
        CancellationToken ct = default)
    {
        var settings = await _notificationsSettingsRepository
            .GetByIdOrDefaultAsync(userId, ct:ct);
        settings.ApplyCurrentLocale();
        
        var strategies = new List<INotificationStrategy>();
        if (settings.PushesEnabled)
        {
            strategies.Add(_pushNotificationStrategy);
        }

        if (settings.EmailsEnabled)
        {
            strategies.Add(_emailNotificationStrategy);
        }

        var runningTasks = new List<Task>();
        foreach (var strategy in strategies)
        {
            var task = sendFunc.Invoke(strategy);
            runningTasks.Add(task);
        }
        await Task.WhenAll(runningTasks);
    }
}