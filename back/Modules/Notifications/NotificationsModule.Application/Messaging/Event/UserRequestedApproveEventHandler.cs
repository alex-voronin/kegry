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
public class UserRequestedApproveEventHandler : DebouncedEventHandlerBase<UserRequestedApproveEvent>, IScoped
{
    private readonly INotificationManager _notificationManager;
    private readonly INotificationRepository _notificationRepository;
    private readonly INotificationsSettingsRepository _notificationsSettingsRepository;
    private readonly IStringLocalizer<NotificationsLocalization> _localizer;

    public UserRequestedApproveEventHandler(
        TimeProvider timeProvider,
        HybridCache cache,
        IMessageBus messageBus,
        INotificationManager notificationManager,
        INotificationRepository notificationRepository,
        INotificationsSettingsRepository notificationsSettingsRepository,
        IStringLocalizer<NotificationsLocalization> localizer) : base(timeProvider, cache, messageBus)
    {
        _notificationManager = notificationManager;
        _notificationRepository = notificationRepository;
        _notificationsSettingsRepository = notificationsSettingsRepository;
        _localizer = localizer;
    }

    [Transactional]
    public override async Task HandleAsync(UserRequestedApproveEvent @event, CancellationToken ct)
    {
        Log.Information("{HandlerClass} Handling event {@Event}",
            typeof(UserRequestedApproveEventHandler).FullName, @event);
        await DebounceEventAsync(@event, ct);
    }

    public override Task HandleAsync(ScheduledEventMessage<UserRequestedApproveEvent> message, CancellationToken ct)
    {
        return HandleScheduledMessageAsync(message, ct);
    }

    protected override async Task HandleDebouncedEventAsync(UserRequestedApproveEvent @event, CancellationToken ct)
    {
        Log.Information("{HandlerClass} Handling debounced event {@Event}",
            typeof(UserRequestedApproveEventHandler).FullName, @event);

        var settings = await _notificationsSettingsRepository.GetByIdOrDefaultAsync(
            id: @event.EventCreatorId,
            asNoTracking: true,
            ct: ct);
        settings.ApplyCurrentLocale();

        var notification = new Notification()
        {
            Id = Guid.CreateVersion7(),
            Type = NotificationType.UserRequestedApprove,
            UserId = @event.EventCreatorId,
            FromUserId = @event.JoinedUserId,
            CreatedAt = TimeProvider.GetUtcNow(),
            ExpiresAt = @event.EventStartDate,
            Text = _localizer.GetString("InApp.UserRequestedApprove.PrimaryText"),
            IsSeen = false,
            LinkedEntityId = @event.EventId
        };
        await _notificationRepository.AddAsync(notification, ct);
        await _notificationManager.SendUserRequestedApprove(@event, ct);
        await _notificationRepository.SaveChangesAsync(ct);
    }
}