using ChatModule.Contracts.Events;

namespace NotificationsModule.Application.Services;

public interface INotificationStrategy
{
    public Task SendMeetingArranged(Guid userId, string otherUserFullName, CancellationToken ct = default);
    public Task SendUserInvited(Guid userId, CancellationToken ct = default);
    public Task SendChatMessageReceived(ChatMessageSentEvent @event, CancellationToken ct = default);
    public Task SendChatMessageReceivedSilent(ChatMessageSentEvent @event, CancellationToken ct = default);
}