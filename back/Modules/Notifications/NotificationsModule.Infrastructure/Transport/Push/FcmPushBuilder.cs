using FirebaseAdmin.Messaging;

namespace NotificationsModule.Infrastructure.Transport.Push;

internal record PushMessage(Message? UnicastMessage, MulticastMessage? MulticastMessage);

internal static class FcmPushBuilder
{
    internal static PushMessage BuildInvitationAcceptedPush(string[] fcmTokens, string userName)
    {
        var notification = new Notification()
        {
            Title = "Invitation accepted!",
            Body = $"{userName} is ready to chat"
        };
        var data = new Dictionary<string, string>()
        {
            { "action", "open_chat" }
        };

        return BuildPushMessages(fcmTokens, notification, data);
    }

    private static PushMessage BuildPushMessages(
        string[] fcmTokens,
        Notification notification,
        Dictionary<string, string> data)
    {
        return fcmTokens.Length > 1
            ? new PushMessage(
                UnicastMessage: null,
                MulticastMessage: new MulticastMessage()
                {
                    Tokens = fcmTokens,
                    Notification = notification,
                    Data = data
                }
            )
            : new PushMessage(
                UnicastMessage: new Message()
                {
                    Token = fcmTokens[0],
                    Notification = notification,
                    Data = data,
                },
                MulticastMessage: null);
    }
}