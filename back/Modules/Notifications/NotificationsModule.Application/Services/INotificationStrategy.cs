using ChatModule.Contracts.Events;
using EventModule.Contracts.Events;

namespace NotificationsModule.Application.Services;

public interface INotificationStrategy
{
    public Task SendMeetingArranged(Guid userId, string otherUserFullName, CancellationToken ct = default);
    public Task SendUserInvited(Guid userId, CancellationToken ct = default);
    public Task SendChatMessageReceived(ChatMessageSentEvent @event, CancellationToken ct = default);
    public Task SendReferrerRewardReceived(Guid userId, CancellationToken ct = default);
    public Task SendParticipationApproved(UserParticipationApprovedEvent @event, CancellationToken ct = default);
    public Task SendUserRequestedApprove(UserRequestedApproveEvent @event, CancellationToken ct = default);
}