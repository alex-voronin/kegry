using Notifications.Infrastructure.Transport.Email;
using Notifications.Infrastructure.Transport.Email.Emails;
using Security.Contracts.Events;
using Wolverine.Attributes;

namespace Notifications.Application.Messaging;

[WolverineHandler]
public static class AccountEventsHandler
{
    public static async Task HandleAsync(AccountCreatedEvent @event, IEmailService emailService)
    {
        var email = new EmailConfirmationEmail()
        {
            ConfirmationCode = @event.ConfirmationCode,
            ToEmail = @event.Email,
        };
        await emailService.SendEmailAsync(email);
    }
}