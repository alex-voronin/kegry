using FeedModule.Contracts.Events;
using Microsoft.Extensions.Caching.Hybrid;
using NotificationsModule.Application.Services;
using SharedCore.Services.LifeTimes;
using Wolverine;
using Wolverine.Attributes;

namespace NotificationsModule.Application.Messaging.Feed;

[WolverineHandler]
public class UserInviteEventHandler : BaseDebouncedEventHandler<UserInvitedEvent>, IScoped
{
    private readonly IPushService _pushService;

    public UserInviteEventHandler(
        TimeProvider timeProvider,
        HybridCache cache,
        IMessageBus messageBus,
        IPushService pushService) : base(timeProvider, cache, messageBus)
    {
        _pushService = pushService;
    }

    public override async Task HandleAsync(UserInvitedEvent @event, CancellationToken ct)
    {
        await DebounceEventAsync(@event, ct);
    }

    public override async Task HandleAsync(ScheduledEventMessage<UserInvitedEvent> message, CancellationToken ct)
    {
        await HandleScheduledMessageAsync(message, ct);
    }
    
    protected override async Task InternalHandleEventAsync(UserInvitedEvent @event, CancellationToken ct)
    {
        await _pushService.SendUserInvitedPush(@event.UserId, ct);
    }
}