using IntegrationTests.Shared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using NotificationsModule.Infrastructure.Resources;
using NotificationsModule.Infrastructure.Transport.Emails;
using NotificationsModule.Infrastructure.Transport.Emails.Configs;

namespace IntegrationTests.NotificationsModule;

[Collection("IntegrationTests")]
public class OneSignalEmailSenderTests : IAsyncLifetime
{
    public Task InitializeAsync() => Task.CompletedTask;
    public Task DisposeAsync() => Task.CompletedTask;

    private readonly IntegrationWebApplicationFactory _factory;

    public OneSignalEmailSenderTests(IntegrationWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact(Skip = "Only for manual testing")]
    public async Task GivenValidConfig_WhenSendEmail_ThenEmailIsSent()
    {
        const string toEmail = "hoppy-test@mailinator.com";

        var emailBuilder = _factory.Services.GetRequiredService<IEmailBuilder>();
        var email = emailBuilder.BuildUserInvitedEmail(toEmail);

        var config = _factory.Services.GetRequiredService<IOptions<EmailConfig>>();
        var localizer = _factory.Services.GetRequiredService<IStringLocalizer<NotificationsLocalization>>();
        var emailSender = new OneSignalEmailSender(config, localizer);
        await emailSender.SendEmailAsync(email, CancellationToken.None);
    }
}
