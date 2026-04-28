using NotificationModule.Domain.Aggregates;
using NotificationModule.Domain.Repositories;
using NotificationsModule.Application.Services;
using SecurityModule.Contracts.Events;
using Serilog;
using Wolverine.Attributes;

namespace NotificationsModule.Application.Messaging.Account;

[WolverineHandler]
public static class AccountEventHandlers
{
    public static async Task HandleAsync(
        AccountCreatedEvent @event,
        IEmailNotificationStrategy emailNotificationStrategy,
        INotificationsSettingsRepository settingsRepository,
        IUserRepository userRepository,
        CancellationToken ct = default)
    {
        var transaction = await userRepository.And(settingsRepository).StartTransactionAsync(ct);

        var user = await userRepository.GetByIdAsync(@event.UserId, ct: ct);
        if (user == null)
        {
            user = new User
            {
                Id = @event.UserId,
                Email = @event.Email,
                FirstName = "",
                LastName = "",
                AvatarImageS3Key = "",
                UpdatedAt = DateTimeOffset.UtcNow
            };
            await userRepository.AddAsync(user, ct);
        }

        var settings = await settingsRepository.GetByIdOrDefaultAsync(@event.UserId, ct: ct);
        settings.Locale = @event.Locale;
        await settingsRepository.SaveAsync(settings, ct);

        await transaction.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        if (@event.ConfirmationCode == null)
        {
            return;
        }

        Log.Information("{HandlerClass} Handling event {@Event}",
            typeof(AccountEventHandlers).FullName, @event);

        // In fact, the user and settings have not been created yet, so we pass a locale string in the event
        settings.ApplyCurrentLocale();
        await emailNotificationStrategy.SendConfirmEmail(
            toEmail: @event.Email,
            confirmationCode: @event.ConfirmationCode,
            ct: ct);
    }

    public static async Task HandleAsync(
        AccountDeletedEvent @event,
        IDeviceRepository deviceRepository,
        CancellationToken ct = default)
    {
        Log.Information("{HandlerClass} Handling event {@Event}",
            typeof(AccountEventHandlers).FullName, @event);

        await deviceRepository.DeleteByUserIdAsync(@event.Id, ct: ct);
        await deviceRepository.SaveChangesAsync(ct);
    }

    public static async Task HandleAsync(
        AccountLoggedOutEvent @event,
        IDeviceRepository deviceRepository,
        CancellationToken ct = default)
    {
        Log.Information("{HandlerClass} Handling event {@Event}",
            typeof(AccountEventHandlers).FullName, @event);

        if (@event.DeviceId != null)
        {
            await deviceRepository.DeleteByIdAsync(@event.DeviceId, @event.UserId, ct: ct);
            await deviceRepository.SaveChangesAsync(ct);
        }
    }
}