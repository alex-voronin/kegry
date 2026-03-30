using System.Globalization;
using FeedModule.Contracts.Events;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Localization;
using NotificationModule.Domain.Aggregates;
using NotificationModule.Domain.Repositories;
using NotificationModule.Domain.ValueObjects;
using NotificationsModule.Application.Services;
using NotificationsModule.Infrastructure.Resources;
using Serilog;
using SharedCore.Localization;
using SharedCore.Services.LifeTimes;
using Wolverine;
using Wolverine.Attributes;

namespace NotificationsModule.Application.Messaging.Feed;

[WolverineHandler]
public class UserInvitedEventHandler : DebouncedEventHandlerBase<UserInvitedEvent>, IScoped
{
    private readonly IStringLocalizer<NotificationsLocalization> _localizer;
    private readonly INotificationRepository _notificationRepository;
    private readonly INotificationManager _notificationManager;
    private readonly INotificationsSettingsRepository _notificationsSettingsRepository;

    public UserInvitedEventHandler(
        TimeProvider timeProvider,
        HybridCache cache,
        IMessageBus messageBus,
        IStringLocalizer<NotificationsLocalization> localizer,
        INotificationRepository notificationRepository,
        INotificationsSettingsRepository notificationsSettingsRepository,
        INotificationManager notificationManager) : base(timeProvider, cache, messageBus)
    {
        _localizer = localizer;
        _notificationRepository = notificationRepository;
        _notificationManager = notificationManager;
        _notificationsSettingsRepository = notificationsSettingsRepository;
    }

    [Transactional]
    public override async Task HandleAsync(UserInvitedEvent @event, CancellationToken ct)
    {
        Log.Information("{HandlerClass} Handling event {@Event}",
            typeof(UserInvitedEventHandler).FullName, @event);

        var settings = await _notificationsSettingsRepository.GetByIdOrDefaultAsync(
            id: @event.UserId, 
            asNoTracking: true,
            ct:ct);
       settings.ApplyCurrentLocale();
        
        var notification = new Notification()
        {
            Id = Guid.CreateVersion7(),
            Type = NotificationType.Invite,
            UserId = @event.UserId,
            FromUserId = @event.ByUserId,
            CreatedAt = TimeProvider.GetUtcNow(),
            ExpiresAt = @event.InviteExpiresAt,
            Text = _localizer.GetString("InApp.UserInvitedNotification.PrimaryText"),
            IsSeen = false
        };
        await _notificationRepository.AddAsync(notification, ct);
        await DebounceEventAsync(@event, ct);
        // in fact SaveChangesAsync do nothing here as whole method marked as [transactional] and wrapped
        // into a transaction scope. It's a requirement of Wolverine's transactional inboxes.
        await _notificationRepository.SaveChangesAsync(ct);
    }

    public override Task HandleAsync(ScheduledEventMessage<UserInvitedEvent> message, CancellationToken ct)
    {
        return HandleScheduledMessageAsync(message, ct);
    }

    protected override Task HandleDebouncedEventAsync(UserInvitedEvent @event, CancellationToken ct)
    {
        Log.Information("{HandlerClass} Handling debounced event {@Event}",
            typeof(UserInvitedEventHandler).FullName, @event);
        return _notificationManager.SendUserInvited(@event.UserId, ct);
    }
}