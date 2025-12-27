using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Options;
using SharedCore.Services.LifeTimes;

namespace Notifications.Infrastructure.Transport.Email;

internal interface IEmailSender
{
    internal Task SendEmailAsync(string toEmail, string subject, string? htmlContent, string? textContent = "");
}

internal class MailgunEmailSender : IEmailSender, ISingleton
{
    private readonly EmailConfig _emailConfig;
    private readonly HttpClient _httpClient;

    public MailgunEmailSender(IOptions<EmailConfig> emailSettings, IHttpClientFactory httpClientFactory)
    {
        _emailConfig = emailSettings.Value;
        _httpClient = httpClientFactory.CreateClient();

        var authToken = Encoding.ASCII.GetBytes($"api:{_emailConfig.ApiKey}");
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Basic", Convert.ToBase64String(authToken));
    }

    public async Task SendEmailAsync(string toEmail, string subject, string? htmlContent, string? textContent = "")
    {
        var formContent = new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                { "from", $"Hoppyfy.me <{_emailConfig.FromEmail}>" },
                { "to", toEmail },
                { "subject", subject },
                { "text", textContent ?? "" },
                { "html", htmlContent ?? "" }
            });

        using var result = await _httpClient.PostAsync(
            $"https://api.mailgun.net/v3/{_emailConfig.Domain}/messages",
            formContent);
        result.EnsureSuccessStatusCode();
    }
}