using NotificationsModule.Infrastructure.Transport.Push;
using NotificationsModule.Infrastructure.Transport.Emails;
using NSubstitute;

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

    public Task AwaitPushNotifications(
        Func<Task> codeToRun,
        int expectedCount,
        string? expectedType = null,
        int timeoutSeconds = 5)
    {
        return AwaitPushNotifications(
            codeToRun: codeToRun,
            pushPredicate: p => p.UnicastMessage!.Notification == null &&
                                (
                                    expectedType == null ||
                                    p.UnicastMessage!.Data[FcmPushBuilder.Keys.Type] == expectedType
                                ),
            expectedCount: expectedCount,
            timeoutSeconds: timeoutSeconds);
    }

    public async Task AwaitPushNotifications(
        Func<Task> codeToRun,
        Predicate<PushMessage> pushPredicate,
        int expectedCount,
        int timeoutSeconds = 5)
    {
        var eventProcessedTcs = new TaskCompletionSource<bool>();
        var notificationsCount = 0;

        PushSender.SendPushMessage(Arg.Any<PushMessage>())
            .Returns(args =>
            {
                if (args[0] is PushMessage pushMessage && pushPredicate(pushMessage))
                {
                    if (Interlocked.Increment(ref notificationsCount) >= expectedCount)
                    {
                        eventProcessedTcs.TrySetResult(true);
                    }
                }

                return new SendResult(true, []);
            });

        await codeToRun();
        await eventProcessedTcs.Task.WaitAsync(TimeSpan.FromSeconds(timeoutSeconds));
    }
}