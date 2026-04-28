using Microsoft.Extensions.Caching.Hybrid;
using NotificationModule.Domain.Repositories;
using NotificationsModule.Application.Services;
using SecurityModule.Contracts.Events;
using Serilog;
using SharedCore.Services.LifeTimes;
using Wolverine;
using Wolverine.Attributes;

namespace NotificationsModule.Application.Messaging.Account;

[WolverineHandler]
public class PasswordResetRequestedEventHandler : DebouncedEventHandlerBase<PasswordResetRequestedEvent>,
    IScoped
{
    private readonly IEmailNotificationStrategy _emailNotificationStrategy;
    private readonly INotificationsSettingsRepository _settingsRepository;

    public PasswordResetRequestedEventHandler(
        TimeProvider timeProvider,
        HybridCache cache,
        IMessageBus messageBus,
        IEmailNotificationStrategy emailNotificationStrategy,
        INotificationsSettingsRepository settingsRepository) : base(timeProvider, cache, messageBus)
    {
        DebounceDurationSeconds = 30;
        _emailNotificationStrategy = emailNotificationStrategy;
        _settingsRepository = settingsRepository;
    }

    [Transactional]
    public override async Task HandleAsync(PasswordResetRequestedEvent @event, CancellationToken ct)
    {
        Log.Information("{HandlerClass} Handling event {@Event}",
            typeof(PasswordResetRequestedEventHandler).FullName, @event);
        await DebounceEventAsync(@event, ct);
    }

    public override Task HandleAsync(ScheduledEventMessage<PasswordResetRequestedEvent> message,
        CancellationToken ct)
    {
        return HandleScheduledMessageAsync(message, ct);
    }

    protected override async Task HandleDebouncedEventAsync(PasswordResetRequestedEvent @event,
        CancellationToken ct)
    {
        Log.Information("{HandlerClass} Handling debounced event {@Event}",
            typeof(PasswordResetRequestedEventHandler).FullName, @event);

        var settings = await _settingsRepository.GetByIdOrDefaultAsync(@event.UserId, asNoTracking: true, ct: ct);
        settings.ApplyCurrentLocale();
        await _emailNotificationStrategy.SendPasswordReset(
            toEmail: @event.Email,
            baseUrl: @event.BaseUrl,
            resetToken: @event.ResetToken,
            ct: ct);
    }
}