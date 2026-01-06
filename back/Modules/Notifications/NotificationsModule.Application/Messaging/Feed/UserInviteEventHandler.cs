using FeedModule.Contracts.Events;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Localization;
using NotificationModule.Domain.Aggregates;
using NotificationModule.Domain.Repositories;
using NotificationsModule.Application.Services;
using NotificationsModule.Infrastructure.Resources;
using SharedCore.Localization;
using SharedCore.Services.LifeTimes;
using Wolverine;
using Wolverine.Attributes;

namespace NotificationsModule.Application.Messaging.Feed;

[WolverineHandler]
public class UserInviteEventHandler : BaseDebouncedEventHandler<UserInvitedEvent>, IScoped
{
    private readonly IDateTimeHumanizer _dateTimeHumanizer;
    private readonly IStringLocalizer<NotificationsLocalization> _localizer;
    private readonly INotificationRepository _notificationRepository;
    private readonly INotificationManager _notificationManager;

    public UserInviteEventHandler(
        TimeProvider timeProvider,
        HybridCache cache,
        IMessageBus messageBus,
        IDateTimeHumanizer dateTimeHumanizer,
        IStringLocalizer<NotificationsLocalization> localizer,
        INotificationRepository notificationRepository,
        INotificationManager notificationManager) : base(timeProvider, cache, messageBus)
    {
        _dateTimeHumanizer = dateTimeHumanizer;
        _localizer = localizer;
        _notificationRepository = notificationRepository;
        _notificationManager = notificationManager;
    }

    [Transactional]
    public override async Task HandleAsync(UserInvitedEvent @event, CancellationToken ct)
    {
        var notification = new Notification
        {
            Id = Guid.CreateVersion7(),
            UserId = @event.UserId,
            FromUserId = @event.ByUserId,
            CreatedAt = TimeProvider.GetUtcNow(),
            PrimaryText = _localizer.GetString("InApp.UserInvitedNotification.PrimaryText"),
            SecondaryText = _dateTimeHumanizer.Humanize(@event.MeetingExpiresAt), 
            IsSeen = false
        };
        await _notificationRepository.AddAsync(notification, ct);
        await DebounceEventAsync(@event, ct);
        // in fact SaveChangesAsync do nothing here as whole method marked as [transactional] and wrapped
        // into a transaction scope. It's a requirement of Wolverine's transactional inboxes.
        await _notificationRepository.SaveChangesAsync(ct);
    }

    public override async Task HandleAsync(ScheduledEventMessage<UserInvitedEvent> message, CancellationToken ct)
    {
        await HandleScheduledMessageAsync(message, ct);
    }

    protected override async Task HandleDebouncedEventAsync(UserInvitedEvent @event, CancellationToken ct)
    {
        await _notificationManager.SendUserInvited(@event.UserId, ct);
    }
}