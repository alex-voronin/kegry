using ChatModule.Contracts.Events;
using FirebaseAdmin.Messaging;
using Microsoft.Extensions.Localization;
using NotificationsModule.Infrastructure.Resources;
using SharedCore.Services.LifeTimes;

namespace NotificationsModule.Infrastructure.Transport.Push;

public record PushMessage(Message? UnicastMessage, MulticastMessage? MulticastMessage);

public interface IPushBuilder
{
    PushMessage BuildMeetingArrangedPush(string[] fcmTokens, string otherUserFullName);
    PushMessage BuildUserInvitedPush(string[] fcmTokens);
    PushMessage BuildChatMessageReceivedPush(string[] fcmTokens, ChatMessageSentEvent @event);
    PushMessage BuildChatMessageReceivedSilentPush(string[] fcmTokens, ChatMessageSentEvent @event);
    PushMessage BuildOpenRateUserScreenSilentPush(string[] fcmTokens, Guid rateeUserId);
    PushMessage BuildEndMeetingSuggestionSilentPush(string[] fcmTokens);
}

internal class FcmPushBuilder : IPushBuilder, ISingleton
{
    internal static class Keys
    {
        public const string Action = "action";
        public const string UserAction = "userAction";
        public const string FromUserId = "fromUserId";
        public const string MessageText = "messageText";
        public const string CreatedAt = "createdAt";
        public const string RateeId = "rateeId";
    }

    // Actions are commands to the frontend that must be performed 
    internal static class Actions
    {
        public const string OpenChatScreen = "openChatScreen";
        public const string OpenRateUserScreen = "openRateUserScreen";
        public const string ShowEndMeetingSuggestion = "showEndMeetingSuggestion";
    }

    // User actions triggered by tapping a push notification 
    internal static class UserActions
    {
        public const string OpenNotificationsScreen = "openNotificationsScreen";
        public const string OpenChatScreen = "openChatScreen";
    }

    private readonly IStringLocalizer<NotificationsLocalization> _localizer;

    public FcmPushBuilder(IStringLocalizer<NotificationsLocalization> localizer)
    {
        _localizer = localizer;
    }

    public PushMessage BuildMeetingArrangedPush(string[] fcmTokens, string otherUserFullName)
    {
        var title = _localizer.GetString("Push.MeetingArranged.Title");
        var body = _localizer.GetString("Push.MeetingArranged.Body", otherUserFullName);

        var notification = new Notification()
        {
            Title = title,
            Body = body
        };
        var data = new Dictionary<string, string>()
        {
            { Keys.Action, Actions.OpenChatScreen }
        };

        return BuildPushMessages(fcmTokens, notification, data);
    }

    public PushMessage BuildUserInvitedPush(string[] fcmTokens)
    {
        var title = _localizer.GetString("Push.UserInvited.Title");
        var body = _localizer.GetString("Push.UserInvited.Body");

        var notification = new Notification()
        {
            Title = title,
            Body = body
        };
        var data = new Dictionary<string, string>()
        {
            { Keys.UserAction, UserActions.OpenNotificationsScreen }
        };

        return BuildPushMessages(fcmTokens, notification, data);
    }

    public PushMessage BuildChatMessageReceivedPush(string[] fcmTokens, ChatMessageSentEvent @event)
    {
        var title = _localizer.GetString("Push.ChatMessageReceived.Title");
        var body = _localizer.GetString("Push.ChatMessageReceived.Body", @event.FromUserName);

        var notification = new Notification()
        {
            Title = title,
            Body = body,
        };
        var webPush = new WebpushConfig()
        {
            Notification = new WebpushNotification()
            {
                Title = title,
                Body = body,
                Icon = @event.FromUserAvatarUrl
            }
        };
        var data = new Dictionary<string, string>()
        {
            { Keys.UserAction, UserActions.OpenChatScreen },
            { Keys.FromUserId, @event.FromUserId.ToString() },
            { Keys.MessageText, @event.MessageText },
            { Keys.CreatedAt, @event.CreatedAt.ToString("o") },
        };

        return BuildPushMessages(fcmTokens, notification, data, webPush);
    }

    public PushMessage BuildChatMessageReceivedSilentPush(string[] fcmTokens, ChatMessageSentEvent @event)
    {
        var data = new Dictionary<string, string>()
        {
            { Keys.UserAction, UserActions.OpenChatScreen },
            { Keys.FromUserId, @event.FromUserId.ToString() },
            { Keys.MessageText, @event.MessageText },
            { Keys.CreatedAt, @event.CreatedAt.ToString("o") },
        };

        return BuildPushMessages(fcmTokens, null, data);
    }

    public PushMessage BuildOpenRateUserScreenSilentPush(string[] fcmTokens, Guid rateeUserId)
    {
        var data = new Dictionary<string, string>()
        {
            { Keys.Action, Actions.OpenRateUserScreen },
            { Keys.RateeId, rateeUserId.ToString() },
        };
        return BuildPushMessages(fcmTokens, null, data);
    }

    public PushMessage BuildEndMeetingSuggestionSilentPush(string[] fcmTokens)
    {
        var data = new Dictionary<string, string>()
        {
            { Keys.Action, Actions.ShowEndMeetingSuggestion }
        };
        return BuildPushMessages(fcmTokens, null, data);
    }

    private static PushMessage BuildPushMessages(
        string[] fcmTokens,
        Notification? notification,
        Dictionary<string, string> data,
        WebpushConfig? webPushConfig = null)
    {
        // Required for a frontend deduplication logic 
        data["messageId"] = Guid.NewGuid().ToString();

        return fcmTokens.Length > 1
            ? new PushMessage(
                UnicastMessage: null,
                MulticastMessage: new MulticastMessage()
                {
                    Tokens = fcmTokens,
                    Notification = notification,
                    Data = data,
                    Webpush = webPushConfig
                }
            )
            : new PushMessage(
                UnicastMessage: new Message()
                {
                    Token = fcmTokens[0],
                    Notification = notification,
                    Data = data,
                    Webpush = webPushConfig
                },
                MulticastMessage: null);
    }
}