using NotificationsModule.Infrastructure.Transport.Push;
using NotificationsModule.Infrastructure.Transport.Emails;
using NSubstitute;
using Shouldly;

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

    public async Task<int> AwaitPushNotifications(int count, string title, int timeoutSeconds = 5)
    {
        var eventProcessedTcs = new TaskCompletionSource<bool>();
        var notificationsCount = 0;
        const int expectedCalls = 2;

        PushSender.SendPushMessage(Arg.Any<PushMessage>())
            .Returns(args =>
            {
                if (args[0] is PushMessage pushMessage &&
                    pushMessage.UnicastMessage!.Notification.Title == title)
                {
                    if (Interlocked.Increment(ref notificationsCount) == expectedCalls)
                    {
                        eventProcessedTcs.TrySetResult(true);
                    }
                }

                return new SendResult(true, []);
            });

        await eventProcessedTcs.Task.WaitAsync(TimeSpan.FromSeconds(timeoutSeconds));
        return expectedCalls;
    }
}