using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Options;
using NotificationsModule.Infrastructure.Transport.Email.Configs;
using SharedCore.Services.LifeTimes;

namespace NotificationsModule.Infrastructure.Transport.Emails;

public interface IEmailSender
{
    public Task SendEmailAsync(Email email, CancellationToken ct);
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

    public async Task SendEmailAsync(Email email, CancellationToken ct)
    {
        var formContent = new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                { "from", $"Hoppyfy.me <{_emailConfig.FromEmail}>" },
                { "to", email.ToEmail },
                { "subject", email.Subject },
                { "text", email.IsHtml ? "" : email.Body },
                { "html", email.IsHtml ? email.Body : "" }
            });

        using var result = await _httpClient.PostAsync(
            $"https://api.mailgun.net/v3/{_emailConfig.Domain}/messages",
            formContent, ct);
        result.EnsureSuccessStatusCode();
    }
}