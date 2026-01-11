using ChatModule.Contracts.Events;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Localization;
using NotificationsModule.Application.Services;
using NotificationsModule.Infrastructure.Resources;
using SharedCore.Services.LifeTimes;
using Wolverine;
using Wolverine.Attributes;

namespace NotificationsModule.Application.Messaging.Chat;

[WolverineHandler]
public class ChatMessageSentEventHandler : BaseDebouncedEventHandler<ChatMessageSentEvent>, IScoped
{
    private readonly INotificationManager _notificationManager;

    public ChatMessageSentEventHandler(
        TimeProvider timeProvider,
        HybridCache cache,
        IMessageBus messageBus,
        INotificationManager notificationManager) : base(timeProvider, cache, messageBus)
    {
        _notificationManager = notificationManager;
        DebounceDurationSeconds = 180;
    }
    
    public override async Task HandleAsync(ChatMessageSentEvent @event, CancellationToken ct)
    {
        var isEventHandled = await DebounceEventAsync(@event, ct);
        if (!isEventHandled)
        {
            await _notificationManager.SendChatMessageReceivedSilent(@event, ct);
        }
    }

    public override Task HandleAsync(ScheduledEventMessage<ChatMessageSentEvent> message, CancellationToken ct)
    {
        return HandleScheduledMessageAsync(message, ct);
    }
    
    protected override Task HandleDebouncedEventAsync(ChatMessageSentEvent @event, CancellationToken ct)
    {
        return _notificationManager.SendChatMessageReceived(@event, ct);
    }
}