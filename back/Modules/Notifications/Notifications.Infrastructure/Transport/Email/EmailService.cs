using Notifications.Infrastructure.Transport.Email.Emails;
using SharedCore.Services.LifeTimes;

namespace Notifications.Infrastructure.Transport.Email;

public interface IEmailService
{
    public Task SendEmailAsync(AbstractEmail email);
}

internal class EmailService : IEmailService, ISingleton
{
    private readonly IEmailSender _emailSender;

    public EmailService(IEmailSender emailSender)
    {
        _emailSender = emailSender;
    }

    public async Task SendEmailAsync(AbstractEmail email)
    {
        await _emailSender.SendEmailAsync(
            email.ToEmail,
            email.Subject,
            email.IsHtml ? email.BuildBody() : null,
            email.IsHtml ? null : email.BuildBody()
        );
    }
}