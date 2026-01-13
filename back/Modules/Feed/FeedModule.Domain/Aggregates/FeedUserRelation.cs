using SharedCore.Validation;

namespace FeedModule.Domain.Aggregates;

public class FeedUserRelation
{
    public required Guid Id { get; init; }
    public required Guid MyUserId { get; init; }
    public required Guid OtherUserId { get; init; }

    public decimal? Rating
    {
        get;
        set
        {
            Validation.ThrowIfInvalid(
                nameof(Rating),
                () => value == null || (value >= 0.5m && value <= 5 && value % 0.5m == 0),
                "Rating value must be between 0.5 and 5 with 0.5 step"
            );
            field = value;
        }
    }

    public bool IsBlocked { get; set; }
    public bool IsInMeeting { get; set; }
    public bool ShouldRate { get; set; }
    public required DateTimeOffset MeetingExpiresAt { get; set; }
    public required DateTimeOffset SkippedAt { get; set; }
    public required DateTimeOffset LastSeenAt { get; set; }

    public bool IsMeetingExpired(DateTimeOffset dateTime) => MeetingExpiresAt < dateTime;
}