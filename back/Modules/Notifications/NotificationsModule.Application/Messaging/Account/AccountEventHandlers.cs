using NotificationsModule.Application.Services;
using SecurityModule.Contracts.Events;
using Wolverine.Attributes;

namespace NotificationsModule.Application.Messaging.Account;

[WolverineHandler]
public static class AccountEventHandlers
{
    public static async Task HandleAsync(
        AccountCreatedEvent @event,
        IEmailNotificationStrategy emailNotificationStrategy)
    {
        await emailNotificationStrategy.SendConfirmEmail(@event.Email, @event.ConfirmationCode);
    }
}