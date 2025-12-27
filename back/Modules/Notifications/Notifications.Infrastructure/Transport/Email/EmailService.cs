using Notifications.Infrastructure.Transport.Email.Emails;

namespace Notifications.Infrastructure.Transport.Email;

public interface IEmailService
{
    public Task SendEmailAsync(AbstractEmail email);
}

public class EmailService : IEmailService
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