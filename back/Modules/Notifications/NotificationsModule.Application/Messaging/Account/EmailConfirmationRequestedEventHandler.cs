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
public class EmailConfirmationRequestedEventHandler : DebouncedEventHandlerBase<EmailConfirmationRequestedEvent>,
    IScoped
{
    private readonly IEmailNotificationStrategy _emailNotificationStrategy;
    private readonly INotificationsSettingsRepository _settingsRepository;

    public EmailConfirmationRequestedEventHandler(
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
    public override async Task HandleAsync(EmailConfirmationRequestedEvent @event, CancellationToken ct)
    {
        Log.Information("{HandlerClass} Handling event {@Event}",
            typeof(EmailConfirmationRequestedEventHandler).FullName, @event);
        await DebounceEventAsync(@event, ct);
    }

    public override Task HandleAsync(ScheduledEventMessage<EmailConfirmationRequestedEvent> message,
        CancellationToken ct)
    {
        return HandleScheduledMessageAsync(message, ct);
    }

    protected override async Task HandleDebouncedEventAsync(EmailConfirmationRequestedEvent @event,
        CancellationToken ct)
    {
        Log.Information("{HandlerClass} Handling debounced event {@Event}",
            typeof(EmailConfirmationRequestedEventHandler).FullName, @event);

        var settings = await _settingsRepository.GetByIdOrDefaultAsync(@event.UserId, asNoTracking: true, ct: ct);
        settings.ApplyCurrentLocale();
        await _emailNotificationStrategy.SendConfirmEmail(
            toEmail: @event.Email,
            confirmationCode: @event.ConfirmationCode,
            ct: ct);
    }
}