namespace NotificationsModule.Application.Services;

public interface INotificationStrategy
{
    public Task SendMeetingArranged(Guid userId, string otherUserName, CancellationToken ct = default);
    public Task SendUserInvited(Guid userId, CancellationToken ct = default);
}