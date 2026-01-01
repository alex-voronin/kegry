using NetTopologySuite.Geometries;

namespace FeedModule.Domain.Aggregates.ReadOnly;

public class Location
{
    public Guid UserId { get; init; }
    public Point Coordinates { get; init; } = null!;
    public DateTime Timestamp { get; init; }

    internal Location()
    {
    }
}