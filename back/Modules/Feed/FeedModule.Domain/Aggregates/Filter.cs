using FeedModule.Domain.ValueObjects;
using SharedCore.ValueObjects;

namespace FeedModule.Domain.Aggregates;

public class Filter
{
    public required Guid UserId { get; init; }
    public required int RadiusMeters { get; init; }
    public required MeetingDeadline MeetingDeadline { get; init; }
    public required int? AgeFrom { get; init; }
    public required int? AgeTo { get; init; }
    public required Gender? Gender { get; init; }
    public required Guid[] HobbyIds  { get; init; }

    public static Filter CreateDefault(Guid userId)
    {
        return new Filter
        {
            UserId = userId,
            RadiusMeters = 30000,
            MeetingDeadline = MeetingDeadline.Tomorrow,
            AgeFrom = null,
            AgeTo = null,
            Gender = null,
            HobbyIds = []
        };
    }
    
    public int GetMeetingExpirationMinutesFromNow()
    {
        var now = DateTimeOffset.UtcNow;
        return MeetingDeadline switch
        {
            MeetingDeadline.Now => 240,
            MeetingDeadline.Today => (int)(now.Date.AddDays(1) - now.DateTime).TotalMinutes,
            MeetingDeadline.Tomorrow => (int)(now.Date.AddDays(2) - now.DateTime).TotalMinutes,
            _ => throw new ArgumentOutOfRangeException(nameof(MeetingDeadline), MeetingDeadline, null)
        };
    }
}