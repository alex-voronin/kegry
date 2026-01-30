using ChatModule.Contracts.Events;
using FirebaseAdmin.Messaging;
using JasperFx.Core;
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
    internal static class PushType
    {
        public static readonly string MeetingArranged = nameof(MeetingArranged).ToCamelCase();
        public static readonly string UserInvited = nameof(UserInvited).ToCamelCase();
        public static readonly string ChatMessageReceived = nameof(ChatMessageReceived).ToCamelCase();
        public static readonly string OpenRateUserScreen = nameof(OpenRateUserScreen).ToCamelCase();
        public static readonly string ShowEndMeetingSuggestion = nameof(ShowEndMeetingSuggestion).ToCamelCase();
    }
    
    internal static class Keys
    {
        public static readonly string FromUserId = nameof(FromUserId).ToCamelCase();
        public static readonly string MessageText = nameof(MessageText).ToCamelCase();
        public static readonly string CreatedAt = nameof(CreatedAt).ToCamelCase();
        public static readonly string RateeId = nameof(RateeId).ToCamelCase();
        public static readonly string Type = nameof(Type).ToCamelCase();
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
            { Keys.Type, PushType.MeetingArranged },
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
            { Keys.Type, PushType.UserInvited },
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
            { Keys.FromUserId, @event.FromUserId.ToString() },
            { Keys.MessageText, @event.MessageText },
            { Keys.CreatedAt, @event.CreatedAt.ToString("o") },
            { Keys.Type, PushType.ChatMessageReceived },
        };

        return BuildPushMessages(fcmTokens, notification, data, webPush);
    }

    public PushMessage BuildChatMessageReceivedSilentPush(string[] fcmTokens, ChatMessageSentEvent @event)
    {
        var data = new Dictionary<string, string>()
        {
            { Keys.FromUserId, @event.FromUserId.ToString() },
            { Keys.MessageText, @event.MessageText },
            { Keys.CreatedAt, @event.CreatedAt.ToString("o") },
            { Keys.Type, PushType.ChatMessageReceived },
        };

        return BuildPushMessages(fcmTokens, null, data);
    }

    public PushMessage BuildOpenRateUserScreenSilentPush(string[] fcmTokens, Guid rateeUserId)
    {
        var data = new Dictionary<string, string>()
        {
            { Keys.RateeId, rateeUserId.ToString() },
            { Keys.Type, PushType.OpenRateUserScreen },
        };
        return BuildPushMessages(fcmTokens, null, data);
    }

    public PushMessage BuildEndMeetingSuggestionSilentPush(string[] fcmTokens)
    {
        var data = new Dictionary<string, string>()
        {
            { Keys.Type, PushType.ShowEndMeetingSuggestion },
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