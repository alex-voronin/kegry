using EventModule.Contracts.Events;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Localization;
using NotificationModule.Domain.Aggregates;
using NotificationModule.Domain.Repositories;
using NotificationModule.Domain.ValueObjects;
using NotificationsModule.Application.Services;
using NotificationsModule.Infrastructure.Resources;
using Serilog;
using SharedCore.Services.LifeTimes;
using Wolverine;
using Wolverine.Attributes;

namespace NotificationsModule.Application.Messaging.Event;

[WolverineHandler]
public class UserParticipationApprovedEventHandler : DebouncedEventHandlerBase<UserParticipationApprovedEvent>, IScoped
{
    private readonly INotificationManager _notificationManager;
    private readonly INotificationRepository _notificationRepository;
    private readonly IStringLocalizer<NotificationsLocalization> _localizer;

    public UserParticipationApprovedEventHandler(
        TimeProvider timeProvider,
        HybridCache cache,
        IMessageBus messageBus,
        INotificationManager notificationManager,
        INotificationRepository notificationRepository,
        IStringLocalizer<NotificationsLocalization> localizer) : base(timeProvider, cache, messageBus)
    {
        _notificationManager = notificationManager;
        _notificationRepository = notificationRepository;
        _localizer = localizer;
    }

    [Transactional]
    public override async Task HandleAsync(UserParticipationApprovedEvent @event, CancellationToken ct)
    {
        Log.Information("{HandlerClass} Handling event {@Event}",
            typeof(UserParticipationApprovedEventHandler).FullName, @event);
        await DebounceEventAsync(@event, ct);
    }

    public override Task HandleAsync(ScheduledEventMessage<UserParticipationApprovedEvent> message,
        CancellationToken ct)
    {
        return HandleScheduledMessageAsync(message, ct);
    }

    protected override async Task HandleDebouncedEventAsync(UserParticipationApprovedEvent @event, CancellationToken ct)
    {
        Log.Information("{HandlerClass} Handling debounced event {@Event}",
            typeof(UserParticipationApprovedEventHandler).FullName, @event);
        var notification = new Notification()
        {
            Id = Guid.CreateVersion7(),
            Type = NotificationType.UserParticipationApproved,
            UserId = @event.EventCreatorId,
            FromUserId = @event.EventCreatorId,
            CreatedAt = TimeProvider.GetUtcNow(),
            ExpiresAt = @event.EventStartDate,
            Text = _localizer.GetString("InApp.UserParticipationApprove.PrimaryText"),
            IsSeen = false,
            LinkedEntityId = @event.EventId
        };
        await _notificationRepository.AddAsync(notification, ct);
        await _notificationManager.SendParticipationApproved(@event, ct);
        await _notificationRepository.SaveChangesAsync(ct);
    }
}