using ChatModule.Contracts.Events;
using NotificationModule.Domain.Aggregates;
using SharedCore.Services.LifeTimes;

namespace NotificationsModule.Application.Services;

public interface INotificationManager : INotificationStrategy;

internal class NotificationManager : INotificationManager, IScoped
{
    internal const bool DefaultPushesEnabled = true;
    internal const bool DefaultEmailsEnabled = false;

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

    public Task SendMeetingArranged(Guid userId, string otherUserName, CancellationToken ct = default)
    {
        return SendNotifications(userId,
            sendFunc: strategy => strategy.SendMeetingArranged(userId, otherUserName, ct),
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

    public Task SendChatMessageReceivedSilent(ChatMessageSentEvent @event, CancellationToken ct = default)
    {
        return _pushNotificationStrategy.SendChatMessageReceivedSilent(@event, ct);
    }

    private async Task SendNotifications(
        Guid userId,
        Func<INotificationStrategy, Task> sendFunc,
        CancellationToken ct = default)
    {
        var notificationsSettings = await _notificationsSettingsRepository.GetByIdAsync(
            userId,
            asNoTracking: true,
            ct: ct
        );
        var pushesEnabled = notificationsSettings?.PushesEnabled ?? DefaultPushesEnabled;
        var emailsEnabled = notificationsSettings?.EmailsEnabled ?? DefaultEmailsEnabled;

        var strategies = new List<INotificationStrategy>();
        if (pushesEnabled)
        {
            strategies.Add(_pushNotificationStrategy);
        }

        if (emailsEnabled)
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