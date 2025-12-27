using System.Net.Http.Headers;
using System.Text;
using Application.Emails.Settings;
using Microsoft.Extensions.Options;

namespace Notifications.Infrastructure.Transport.Email;

public interface IEmailSender
{
    public Task SendEmailAsync(string toEmail, string subject, string? htmlContent, string? textContent = "");
}

public class MailgunEmailSender : IEmailSender
{
    private readonly EmailSettings _emailSettings;
    private readonly HttpClient _httpClient;

    public MailgunEmailSender(IOptions<EmailSettings> emailSettings, HttpClient httpClient)
    {
        _emailSettings = emailSettings.Value;
        _httpClient = httpClient;

        var authToken = Encoding.ASCII.GetBytes($"api:{_emailSettings.ApiKey}");
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Basic", Convert.ToBase64String(authToken));
    }

    public async Task SendEmailAsync(string toEmail, string subject, string? htmlContent, string? textContent = "")
    {
        var formContent = new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                { "from", $"Sphere <{_emailSettings.FromEmail}>" },
                { "to", toEmail },
                { "subject", subject },
                { "text", textContent ?? "" },
                { "html", htmlContent ?? "" }
            });

        using var result = await _httpClient.PostAsync(
            $"https://api.mailgun.net/v3/{_emailSettings.Domain}/messages",
            formContent);
        result.EnsureSuccessStatusCode();
    }
}