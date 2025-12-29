using NotificationsModule.Infrastructure.Transport.Email;
using NotificationsModule.Infrastructure.Transport.Email.Emails;
using SecurityModule.Contracts.Events;
using Wolverine.Attributes;

namespace NotificationsModule.Application.Messaging;

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