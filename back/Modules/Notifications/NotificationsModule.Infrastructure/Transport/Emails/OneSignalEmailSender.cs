using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using NotificationsModule.Infrastructure.Resources;
using NotificationsModule.Infrastructure.Transport.Emails.Configs;
using OneSignalApi.Api;
using OneSignalApi.Client;
using OneSignalApi.Model;
using Polly;
using Polly.Retry;
using Serilog;
using SharedCore.Services.LifeTimes;

namespace NotificationsModule.Infrastructure.Transport.Emails;

public interface IEmailSender
{
    public Task SendEmailAsync(Email email, CancellationToken ct);
}

internal class OneSignalEmailSender : IEmailSender, ISingleton
{
    private readonly EmailConfig _emailConfig;
    private readonly DefaultApi _onesignalApi;
    private readonly IStringLocalizer<NotificationsLocalization> _localizer;
    private readonly AsyncRetryPolicy _retryPolicy;

    public OneSignalEmailSender(
        IOptions<EmailConfig> emailConfig,
        IStringLocalizer<NotificationsLocalization> localizer)
    {
        _emailConfig = emailConfig.Value;
        _localizer = localizer;

        var config = new Configuration
        {
            AccessToken = _emailConfig.ApiKey
        };
        _onesignalApi = new DefaultApi(config);

        _retryPolicy = Policy
            .Handle<ApiException>(ex =>
                ex.ErrorCode >= 500 || ex.ErrorCode == 429)
            .Or<HttpRequestException>()
            .WaitAndRetryAsync(3, retryAttempt =>
                    TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                (_, timeSpan, retryCount, _) =>
                {
                    Log.Error("Failed to send email. Try {RetryCount}. Waiting for {TimeSpanTotalSeconds}s...",
                        retryCount, timeSpan.TotalSeconds);
                });
    }

    public async Task SendEmailAsync(Email email, CancellationToken ct)
    {
        var idempotencyKey = Guid.NewGuid().ToString();

        var emailNotification = new Notification(appId: _emailConfig.AppId, idempotencyKey: idempotencyKey)
        {
            TargetChannel = Notification.TargetChannelEnum.Email,
            EmailFromAddress = _emailConfig.FromEmail,
            EmailSubject = email.Subject,
            EmailBody = BuildBody(email.Body) + (email.IsImportant ? "" : BuildUnsubscribeLink()),
            IncludeEmailTokens = [email.ToEmail],
            IncludeUnsubscribed = email.IsImportant
        };

        try
        {
            await _retryPolicy.ExecuteAsync(async () =>
            {
                await _onesignalApi.CreateNotificationAsync(emailNotification, cancellationToken: ct);
            });
        }
        catch (ApiException e)
        {
            Log.Error(e, "Unable to send email");
        }
    }

    private static string BuildBody(string text)
    {
        return $"""
                <p style='font-size: 16px'>{text}</p>
                """;
    }

    private string BuildUnsubscribeLink()
    {
        return $"""
               <br><br>
               <hr>
               <p style='font-size: 12px; color: grey;'>
                   {_localizer.GetString("Email.Common.UnsubscribeText")}
                   <a href='[unsubscribe_url]'>
                       {_localizer.GetString("Email.Common.UnsubscribeLink")}
                   </a>
               </p>
               """;
    }
}