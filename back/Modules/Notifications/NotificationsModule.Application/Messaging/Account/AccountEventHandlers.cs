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
        IEmailNotificationStrategy emailNotificationStrategy)
    {
        if (@event.ConfirmationCode == null)
        {
            return;
        }

        Log.Information("{HandlerClass} Handling event {@Event}",
            typeof(AccountEventHandlers).FullName, @event);
        await emailNotificationStrategy.SendConfirmEmail(@event.Email, @event.ConfirmationCode);
    }

    public static async Task HandleAsync(
        PasswordResetRequestedEvent @event,
        IEmailNotificationStrategy emailNotificationStrategy)
    {
        Log.Information("{HandlerClass} Handling event {@Event}",
            typeof(AccountEventHandlers).FullName, @event);
        await emailNotificationStrategy.SendPasswordReset(@event.Email, @event.ResetToken);
    }
}