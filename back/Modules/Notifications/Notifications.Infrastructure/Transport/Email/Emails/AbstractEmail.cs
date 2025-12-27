namespace Notifications.Infrastructure.Transport.Email.Emails;

public abstract class AbstractEmail
{
    public required string ToEmail { get; set; }
    public string Subject { get; set; }
    public bool IsHtml { get; init; } = true;

    public abstract string BuildBody();

    protected AbstractEmail(string subject)
    {
        Subject = subject;
    }
}