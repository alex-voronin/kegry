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
    PushMessage BuildChatMessageReceivedPush(string[] fcmTokens, ChatMessageSentEvent @event, bool silent = false);
    PushMessage BuildOpenRateUserScreenPush(string[] fcmTokens, Guid rateeUserId);
    PushMessage BuildEndMeetingSuggestionPush(string[] fcmTokens);
    PushMessage BuildReferrerRewardReceived(string[] fcmTokens);
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
        public static readonly string ReferrerRewardReceived = nameof(ReferrerRewardReceived).ToCamelCase();
    }

    internal static class Keys
    {
        public static readonly string Title = nameof(Title).ToCamelCase();
        public static readonly string Body = nameof(Body).ToCamelCase();
        public static readonly string Icon = nameof(Icon).ToCamelCase();
        public static readonly string FromUserId = nameof(FromUserId).ToCamelCase();
        public static readonly string MessageText = nameof(MessageText).ToCamelCase();
        public static readonly string CreatedAt = nameof(CreatedAt).ToCamelCase();
        public static readonly string RateeId = nameof(RateeId).ToCamelCase();
        public static readonly string ChatMessageId = nameof(ChatMessageId).ToCamelCase();
        public static readonly string Type = nameof(Type).ToCamelCase();
        public static readonly string MessageId = nameof(MessageId).ToCamelCase();
        public static readonly string IsSilent = nameof(IsSilent).ToCamelCase();
    }

    private readonly IStringLocalizer<NotificationsLocalization> _localizer;
    private readonly WebpushConfig _highPriorityConfig = new WebpushConfig()
    {
        Headers = new Dictionary<string, string>()
        {
            { "Urgency", "high" }
        }
    };

    public FcmPushBuilder(IStringLocalizer<NotificationsLocalization> localizer)
    {
        _localizer = localizer;
    }

    public PushMessage BuildMeetingArrangedPush(string[] fcmTokens, string otherUserFullName)
    {
        var title = _localizer.GetString("Push.MeetingArranged.Title");
        var body = _localizer.GetString("Push.MeetingArranged.Body", otherUserFullName);

        var data = new Dictionary<string, string>()
        {
            { Keys.Title, title },
            { Keys.Body, body },
            { Keys.Type, PushType.MeetingArranged },
        };

        return BuildPushMessages(
            fcmTokens: fcmTokens,
            notification: null,
            data: data,
            webPushConfig: _highPriorityConfig);
    }

    public PushMessage BuildUserInvitedPush(string[] fcmTokens)
    {
        var title = _localizer.GetString("Push.UserInvited.Title");
        var body = _localizer.GetString("Push.UserInvited.Body");

        var data = new Dictionary<string, string>()
        {
            { Keys.Title, title },
            { Keys.Body, body },
            { Keys.Type, PushType.UserInvited },
        };

        return BuildPushMessages(
            fcmTokens: fcmTokens,
            notification: null,
            data: data,
            webPushConfig: _highPriorityConfig);
    }

    public PushMessage BuildChatMessageReceivedPush(string[] fcmTokens, ChatMessageSentEvent @event,
        bool silent = false)
    {
        var title = _localizer.GetString("Push.ChatMessageReceived.Title", @event.FromUserName);
        var body = _localizer.GetString("Push.ChatMessageReceived.Body");

        var data = new Dictionary<string, string>()
        {
            // The same Id squashes messages to the same user in a mobile device's notification center
            { Keys.MessageId, @event.FromUserId.ToString() },
            { Keys.ChatMessageId, @event.MessageId.ToString() },
            { Keys.Title, title },
            { Keys.Body, body },
            { Keys.Icon, @event.FromUserAvatarUrl },
            { Keys.FromUserId, @event.FromUserId.ToString() },
            { Keys.MessageText, @event.MessageText },
            { Keys.CreatedAt, @event.CreatedAt.ToString("o") },
            { Keys.Type, PushType.ChatMessageReceived },
            { Keys.IsSilent, silent ? "true" : "false" }
        };

        return BuildPushMessages(
            fcmTokens: fcmTokens,
            notification: null,
            data: data,
            webPushConfig: _highPriorityConfig);
    }

    public PushMessage BuildOpenRateUserScreenPush(string[] fcmTokens, Guid rateeUserId)
    {
        var title = _localizer.GetString("Push.OpenRateUserScreen.Title");
        var body = _localizer.GetString("Push.OpenRateUserScreen.Body");
        var data = new Dictionary<string, string>()
        {
            { Keys.Title, title },
            { Keys.Body, body },
            { Keys.RateeId, rateeUserId.ToString() },
            { Keys.Type, PushType.OpenRateUserScreen },
        };

        return BuildPushMessages(
            fcmTokens: fcmTokens,
            notification: null,
            data: data);
    }

    public PushMessage BuildEndMeetingSuggestionPush(string[] fcmTokens)
    {
        var title = _localizer.GetString("Push.EndMeetingSuggestion.Title");
        var body = _localizer.GetString("Push.EndMeetingSuggestion.Body");
        var data = new Dictionary<string, string>()
        {
            { Keys.Title, title },
            { Keys.Body, body },
            { Keys.Type, PushType.ShowEndMeetingSuggestion },
        };

        return BuildPushMessages(
            fcmTokens: fcmTokens,
            notification: null,
            data: data);
    }

    public PushMessage BuildReferrerRewardReceived(string[] fcmTokens)
    {
        var title = _localizer.GetString("Push.ReferrerRewardReceived.Title");
        var body = _localizer.GetString("Push.ReferrerRewardReceived.Body");
        var data = new Dictionary<string, string>()
        {
            { Keys.Title, title },
            { Keys.Body, body },
            { Keys.Type, PushType.ReferrerRewardReceived },
        };

        return BuildPushMessages(
            fcmTokens: fcmTokens,
            notification: null,
            data: data);
    }

    private static PushMessage BuildPushMessages(
        string[] fcmTokens,
        Notification? notification,
        Dictionary<string, string> data,
        WebpushConfig? webPushConfig = null)
    {
        // Required for a frontend deduplication logic 
        if (!data.ContainsKey(Keys.MessageId))
        {
            data[Keys.MessageId] = Guid.CreateVersion7().ToString();
        }

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