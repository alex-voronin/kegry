namespace NotificationModule.Domain.Aggregates;

public class User
{
    public required Guid Id { get; init; }
    public required string Email { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string AvatarImageUrl { get; set; } = "";

    public ICollection<Notification> Notifications { get; init; } = new List<Notification>();
    public NotificationsSettings? NotificationsSettings { get; init; }
    public ICollection<Device> Devices { get; init; } = new List<Device>();
}