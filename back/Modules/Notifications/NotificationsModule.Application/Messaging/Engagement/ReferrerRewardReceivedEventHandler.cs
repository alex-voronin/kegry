using EngagementModule.Contracts.Events;
using Microsoft.Extensions.Caching.Hybrid;
using NotificationsModule.Application.Services;
using Serilog;
using SharedCore.Services.LifeTimes;
using Wolverine;
using Wolverine.Attributes;

namespace NotificationsModule.Application.Messaging.Engagement;

[WolverineHandler]
public class ReferrerRewardReceivedEventHandler : DebouncedEventHandlerBase<ReferrerRewardReceivedEvent>, IScoped
{
    private readonly INotificationManager _notificationManager;

    public ReferrerRewardReceivedEventHandler(
        TimeProvider timeProvider,
        HybridCache cache,
        IMessageBus messageBus,
        INotificationManager notificationManager) : base(timeProvider, cache, messageBus)
    {
        _notificationManager = notificationManager;
    }

    [Transactional]
    public override async Task HandleAsync(ReferrerRewardReceivedEvent @event, CancellationToken ct)
    {
        Log.Information("{HandlerClass} Handling event {@Event}",
            typeof(ReferrerRewardReceivedEventHandler).FullName, @event);
        await DebounceEventAsync(@event, ct);
    }

    public override Task HandleAsync(ScheduledEventMessage<ReferrerRewardReceivedEvent> message, CancellationToken ct)
    {
        return HandleScheduledMessageAsync(message, ct);
    }

    protected override Task HandleDebouncedEventAsync(ReferrerRewardReceivedEvent @event, CancellationToken ct)
    {
        Log.Information("{HandlerClass} Handling debounced event {@Event}",
            typeof(ReferrerRewardReceivedEventHandler).FullName, @event);
        return _notificationManager.SendReferrerRewardReceived(@event.UserId, ct);
    }
}