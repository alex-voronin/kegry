using FirebaseAdmin.Messaging;
using Microsoft.Extensions.Localization;
using NotificationsModule.Infrastructure.Resources;
using SharedCore.Services.LifeTimes;

namespace NotificationsModule.Infrastructure.Transport.Push;

internal record PushMessage(Message? UnicastMessage, MulticastMessage? MulticastMessage);

internal interface IFcmPushBuilder
{
    PushMessage BuildInvitationAcceptedPush(string[] fcmTokens, string userName);
}

internal class FcmPushBuilder : IFcmPushBuilder, ISingleton
{
    private readonly IStringLocalizer<Pushes> _localizer;

    public FcmPushBuilder(IStringLocalizer<Pushes> localizer)
    {
        _localizer = localizer;
    }

    public PushMessage BuildInvitationAcceptedPush(string[] fcmTokens, string userName)
    {
        var title = _localizer["InvitationAcceptedTitle"];
        var body = _localizer["InvitationAcceptedBody", userName];

        var notification = new Notification()
        {
            Title = title,
            Body = body
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