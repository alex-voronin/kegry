namespace NotificationModule.Domain.Aggregates;

public class User
{
    public required Guid Id { get; init; }
    public required string Email { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string AvatarImageS3Key { get; set; }
    public required DateTimeOffset UpdatedAt { get; set; }

    public ICollection<Notification> Notifications { get; init; } = new List<Notification>();
    public NotificationsSettings? NotificationsSettings { get; init; }
    public ICollection<Device> Devices { get; init; } = new List<Device>();
}