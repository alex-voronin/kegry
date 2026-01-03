using FirebaseAdmin.Messaging;
using Microsoft.Extensions.Localization;
using NotificationsModule.Infrastructure.Resources;
using SharedCore.Services.LifeTimes;

namespace NotificationsModule.Infrastructure.Transport.Push;

internal record PushMessage(Message? UnicastMessage, MulticastMessage? MulticastMessage);

internal interface IFcmPushBuilder
{
    PushMessage BuildMeetingArrangedPush(string[] fcmTokens, string otherUserName);
    PushMessage BuildUserInvitedPush(string[] fcmTokens);
}

internal class FcmPushBuilder : IFcmPushBuilder, ISingleton
{
    private readonly IStringLocalizer<Pushes> _localizer;

    public FcmPushBuilder(IStringLocalizer<Pushes> localizer)
    {
        _localizer = localizer;
    }

    public PushMessage BuildMeetingArrangedPush(string[] fcmTokens, string otherUserName)
    {
        var title = _localizer["MeetingArrangedTitle"];
        var body = _localizer["MeetingArrangedBody", otherUserName];

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

    public PushMessage BuildUserInvitedPush(string[] fcmTokens)
    {
        var title = _localizer["UserInvitedTitle"];
        var body = _localizer["UserInvitedBody"];

        var notification = new Notification()
        {
            Title = title,
            Body = body
        };
        var data = new Dictionary<string, string>()
        {
            { "user_action", "open_notifications" }
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