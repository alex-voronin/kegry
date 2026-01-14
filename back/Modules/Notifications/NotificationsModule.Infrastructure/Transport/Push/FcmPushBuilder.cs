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
    PushMessage BuildGoToRateUserScreenSilentPush(string[] fcmTokens, Guid rateeUserId);
    PushMessage BuildEndMeetingSuggestionSilentPush(string[] fcmTokens);
}

internal class FcmPushBuilder : IPushBuilder, ISingleton
{
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
            { "action", "openChatScreen" }
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
            { "userAction", "openNotificationsScreen" }
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
            ImageUrl = @event.FromUserAvatarUrl
        };
        var data = new Dictionary<string, string>()
        {
            { "userAction", "openChatScreen" },
            { "fromUserId", @event.FromUserId.ToString() },
            { "messageText", @event.MessageText },
            { "createdAt", @event.CreatedAt.ToString("o") },
        };

        return BuildPushMessages(fcmTokens, notification, data);
    }

    public PushMessage BuildChatMessageReceivedSilentPush(string[] fcmTokens, ChatMessageSentEvent @event)
    {
        var data = new Dictionary<string, string>()
        {
            { "userAction", "openChatScreen" },
            { "fromUserId", @event.FromUserId.ToString() },
            { "messageText", @event.MessageText },
            { "createdAt", @event.CreatedAt.ToString("o") },
        };

        return BuildPushMessages(fcmTokens, null, data);
    }

    public PushMessage BuildGoToRateUserScreenSilentPush(string[] fcmTokens, Guid rateeUserId)
    {
        var data = new Dictionary<string, string>()
        {
            { "userAction", "openRateUserScreen" },
            { "rateeId", rateeUserId.ToString() },
        };
        return BuildPushMessages(fcmTokens, null, data);
    }

    public PushMessage BuildEndMeetingSuggestionSilentPush(string[] fcmTokens)
    {
        var data = new Dictionary<string, string>()
        {
            { "userAction", "showEndMeetingSuggestion" }
        };
        return BuildPushMessages(fcmTokens, null, data);
    }

    private static PushMessage BuildPushMessages(
        string[] fcmTokens,
        Notification? notification,
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