using NotificationsModule.Infrastructure.Transport.Push;
using NotificationsModule.Infrastructure.Transport.Emails;

namespace IntegrationTests.Shared.Managers;

public class NotificationManager
{
    public IEmailSender EmailSender { get; init; }
    public IPushSender PushSender { get; init; }

    public NotificationManager(IEmailSender emailSender, IPushSender pushSender)
    {
        EmailSender = emailSender;
        PushSender = pushSender;
    }
}