using ChatModule.Contracts.Events;
using Microsoft.Extensions.Caching.Hybrid;
using NotificationModule.Domain.Repositories;
using NotificationsModule.Application.Services;
using SharedCore.Services.LifeTimes;
using Wolverine;
using Wolverine.Attributes;

namespace NotificationsModule.Application.Messaging.Chat;

[WolverineHandler]
public class ChatMessageSentEventHandlerEmails : DebouncedEventHandlerBase<ChatMessageSentEvent>, IScoped
{
    private readonly IEmailNotificationStrategy _emailNotificationStrategy;
    private readonly INotificationsSettingsRepository _notificationsSettingsRepository;

    public ChatMessageSentEventHandlerEmails(
        TimeProvider timeProvider,
        HybridCache cache,
        IMessageBus messageBus,
        IEmailNotificationStrategy emailNotificationStrategy,
        INotificationsSettingsRepository notificationsSettingsRepository) : base(timeProvider, cache, messageBus)
    {
        _emailNotificationStrategy = emailNotificationStrategy;
        _notificationsSettingsRepository = notificationsSettingsRepository;
    }

    public override Task HandleAsync(ChatMessageSentEvent @event, CancellationToken ct)
    {
        return DebounceEventAsync(@event, ct);
    }

    public override Task HandleAsync(ScheduledEventMessage<ChatMessageSentEvent> message, CancellationToken ct)
    {
        return HandleScheduledMessageAsync(message, ct);
    }

    protected override async Task HandleDebouncedEventAsync(ChatMessageSentEvent @event, CancellationToken ct)
    {
        var (_, emailsEnabled) = await _notificationsSettingsRepository
            .GetNotificationsEnabledById(@event.ToUserId, ct);
        if (emailsEnabled)
        {
            await _emailNotificationStrategy.SendChatMessageReceived(@event, ct);
        }
    }
}