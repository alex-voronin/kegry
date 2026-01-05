namespace NotificationModule.Domain.Aggregates;

public class Notification
{
    public required Guid Id { get; init; }
    public required Guid UserId { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public Guid? FromUserId { get; init; }
    public User? FromUser { get; init; }
    public required string PrimaryText { get; init; }
    public required string SecondaryText { get; init; }
    public required bool IsSeen { get; set; }
}