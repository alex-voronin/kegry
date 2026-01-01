using FeedModule.Domain.ValueObjects;

namespace FeedModule.Domain.Aggregates;

public class FeedProfile
{
    public required Guid Id { get; init; }
    public required Guid MyUserId { get; init; }
    public required Guid OtherUserId { get; init; }
    public required decimal Rating { get; set; }
    public required bool IsBlocked { get; set; }
    public required DateTimeOffset MeetingTimeExpiresAt { get; set; } 
    public required DateTimeOffset SkippedAt { get; set; }
    public required DateTimeOffset LastSeenAt { get; set; }
}