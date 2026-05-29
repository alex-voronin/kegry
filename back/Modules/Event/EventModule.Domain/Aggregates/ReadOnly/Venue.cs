using NetTopologySuite.Geometries;

namespace EventModule.Domain.Aggregates.ReadOnly;

public class Venue
{
    public Guid Id { get; init; }
    public Guid CompanyId { get; init; }
    public string Address { get; init; } = null!;
    public string? CoverImageS3Key { get; init; }
    public Point Coordinates { get; init; } = null!;
    public string TimeZoneId { get; init; } = null!;
    public bool IsDeleted { get; init; }

    internal Venue()
    {
    }
}
