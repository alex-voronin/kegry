using System.Globalization;
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
        CancellationToken ct = default)
    {
        if (@event.ConfirmationCode == null)
        {
            return;
        }

        Log.Information("{HandlerClass} Handling event {@Event}",
            typeof(AccountEventHandlers).FullName, @event);

        // In fact, the user and settings have not been created yet, so we pass a locale string in the event
        SetCurrentCulture(@event.Locale);
        await emailNotificationStrategy.SendConfirmEmail(
            toEmail: @event.Email,
            confirmationCode: @event.ConfirmationCode,
            ct: ct);
    }

    public static async Task HandleAsync(
        PasswordResetRequestedEvent @event,
        IEmailNotificationStrategy emailNotificationStrategy,
        CancellationToken ct = default)
    {
        Log.Information("{HandlerClass} Handling event {@Event}",
            typeof(AccountEventHandlers).FullName, @event);

        // The user and settings might not have been created yet, so we pass a locale string in the event
        SetCurrentCulture(@event.Locale);
        await emailNotificationStrategy.SendPasswordReset(
            toEmail: @event.Email,
            baseUrl: @event.BaseUrl,
            resetToken: @event.ResetToken,
            ct: ct);
    }

    private static void SetCurrentCulture(string locale)
    {
        var cultureInfo = CultureInfo.GetCultureInfo(locale);
        CultureInfo.CurrentCulture = cultureInfo;
        CultureInfo.CurrentUICulture = cultureInfo;
    }
}