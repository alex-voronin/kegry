namespace NotificationModule.Domain.Aggregates;

public class NotificationsSettings
{
    public required Guid UserId { get; init; }
    public required bool PushesEnabled { get; set; }
    public required bool EmailsEnabled { get; set; }
}