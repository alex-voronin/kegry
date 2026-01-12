using ChatModule.Contracts.Events;
using Microsoft.Extensions.Caching.Hybrid;
using NotificationModule.Domain.Repositories;
using NotificationsModule.Application.Services;
using SharedCore.Services.LifeTimes;
using Wolverine;
using Wolverine.Attributes;

namespace NotificationsModule.Application.Messaging.Chat;

[WolverineHandler]
public class ChatMessageSentEventHandlerPushes : DebouncedEventHandlerBase<ChatMessageSentEvent>, IScoped
{
    private readonly IPushNotificationStrategy _pushNotificationStrategy;
    private readonly INotificationsSettingsRepository _notificationsSettingsRepository;

    public ChatMessageSentEventHandlerPushes(
        TimeProvider timeProvider,
        HybridCache cache,
        IMessageBus messageBus,
        IPushNotificationStrategy pushNotificationStrategy,
        INotificationsSettingsRepository notificationsSettingsRepository) : base(timeProvider, cache, messageBus)
    {
        _pushNotificationStrategy = pushNotificationStrategy;
        _notificationsSettingsRepository = notificationsSettingsRepository;
        DebounceDurationSeconds = 10;
    }

    public override async Task HandleAsync(ChatMessageSentEvent @event, CancellationToken ct)
    {
        var isEventHandled = await DebounceEventAsync(@event, ct);
        if (!isEventHandled)
        {
            // NOTE: Be careful here! This is correct only as long as a user can have a single active chat
            await _pushNotificationStrategy.SendChatMessageReceivedSilent(@event, ct);
        }
    }

    public override Task HandleAsync(ScheduledEventMessage<ChatMessageSentEvent> message, CancellationToken ct)
    {
        return HandleScheduledMessageAsync(message, ct);
    }

    protected override async Task HandleDebouncedEventAsync(ChatMessageSentEvent @event, CancellationToken ct)
    {
        var (pushesEnabled, _) = await _notificationsSettingsRepository
            .GetNotificationsEnabledById(@event.ToUserId, ct);
        if (pushesEnabled)
        {
            await _pushNotificationStrategy.SendChatMessageReceived(@event, ct);
        }
    }
}