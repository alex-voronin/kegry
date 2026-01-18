using Microsoft.Extensions.Caching.Hybrid;
using NotificationsModule.Application.Services;
using Serilog;
using SharedCore.Events;
using SharedCore.Services.LifeTimes;
using Wolverine;

namespace NotificationsModule.Application.Messaging.Chat;

public record MeetingArrangedNotificationEvent : UserEvent
{
    public required string OtherUserFullName { get; init; }
}

public class MeetingArrangedEventHandler : DebouncedEventHandlerBase<MeetingArrangedNotificationEvent>, IScoped
{
    private readonly INotificationManager _notificationManager;

    public MeetingArrangedEventHandler(
        TimeProvider timeProvider,
        HybridCache cache,
        IMessageBus messageBus,
        INotificationManager notificationManager) : base(timeProvider, cache, messageBus)
    {
        _notificationManager = notificationManager;
    }

    public override Task HandleAsync(MeetingArrangedNotificationEvent @event, CancellationToken ct)
    {
        Log.Information("{HandlerClass} Handling event {@Event}",
            typeof(MeetingArrangedEventHandler), @event);
        return DebounceEventAsync(@event, ct);
    }

    public override Task HandleAsync(ScheduledEventMessage<MeetingArrangedNotificationEvent> message, CancellationToken ct)
    {
        return HandleScheduledMessageAsync(message, ct);
    }

    protected override Task HandleDebouncedEventAsync(MeetingArrangedNotificationEvent @event, CancellationToken ct)
    {
        Log.Information("{HandlerClass} Handling debounced event {@Event}",
            typeof(MeetingArrangedEventHandler), @event);
        return _notificationManager.SendMeetingArranged(@event.UserId, @event.OtherUserFullName, ct);
    }
}