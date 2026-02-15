using NotificationModule.Domain.ValueObjects;

namespace NotificationModule.Domain.Aggregates;

public class Notification
{
    public required Guid Id { get; init; }
    public required NotificationType Type { get; init; }
    public required Guid UserId { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset ExpiresAt { get; init; }
    public Guid? FromUserId { get; init; }
    public User? FromUser { get; init; }
    public required string Text { get; init; }
    public required bool IsSeen { get; set; }
}