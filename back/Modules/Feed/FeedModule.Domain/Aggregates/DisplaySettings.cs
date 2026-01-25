using FeedModule.Domain.ValueObjects;

namespace FeedModule.Domain.Aggregates;

public class DisplaySettings
{
    public required Guid UserId { get; init; }
    public required DistanceUnits DistanceUnits { get; set; }
}