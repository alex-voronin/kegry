using IntegrationTests.Shared;
using IntegrationTests.Shared.Managers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using NotificationsModule.Infrastructure.Resources;
using NotificationsModule.Infrastructure.Transport.Emails;
using NotificationsModule.Infrastructure.Transport.Emails.Configs;

namespace IntegrationTests.NotificationsModule;

[Collection("IntegrationTests")]
public class OneSignalEmailSenderTests : IntegrationTestBase, IAsyncLifetime
{
    public OneSignalEmailSenderTests(IntegrationWebApplicationFactory factory) : base(factory)
    {
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact(Skip = "Only for manual testing")]
    public async Task GivenValidConfig_WhenSendEmail_ThenEmailIsSent()
    {
        const string toEmail = "hoppy-test@mailinator.com";

        var emailBuilder = Factory.Services.GetRequiredService<IEmailBuilder>();
        var email = emailBuilder.BuildUserInvitedEmail(toEmail);

        var config = Factory.Services.GetRequiredService<IOptions<EmailConfig>>();
        var localizer = Factory.Services.GetRequiredService<IStringLocalizer<NotificationsLocalization>>();
        var emailSender = new OneSignalEmailSender(config, localizer);
        await emailSender.SendEmailAsync(email, CancellationToken.None);
    }
}
