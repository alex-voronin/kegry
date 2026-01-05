using System.ComponentModel.DataAnnotations.Schema;
using SharedCore.Validation;

namespace FeedModule.Domain.Aggregates;

public class FeedProfile
{
    public required Guid Id { get; init; }
    public required Guid MyUserId { get; init; }
    public required Guid OtherUserId { get; init; }

    public required decimal Rating
    {
        get;
        set
        {
            Validation.ThrowIfInvalid(
                nameof(Rating),
                () => value >= 1 && value <= 5 && value % 0.5m != 0,
                "Rating value must be between 1 and 5 with 0.5 step"
            );
            field = value;
        }
    }

    public required bool IsBlocked { get; set; }
    public required bool IsInMeeting { get; set; }
    public required DateTimeOffset MeetingExpiresAt { get; set; }
    public required DateTimeOffset SkippedAt { get; set; }
    public required DateTimeOffset LastSeenAt { get; set; }
    
    public bool IsMeetingExpired(DateTimeOffset dateTime) => MeetingExpiresAt < dateTime;
}