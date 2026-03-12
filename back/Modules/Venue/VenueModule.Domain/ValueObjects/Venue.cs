using VenueModule.Domain.Aggregates;

namespace VenueModule.Domain.ValueObjects;

using NetTopologySuite.Geometries;

public class Venue
{
    public Guid Id { get; init; }
    public Guid CompanyId { get; init; }
    public Company Company { get; set; } = null!;
    public string? CoverImageS3Key { get; set; }
    public required Point Coordinates { get; set; }
    public string Address { get; set; } = null!;
    public string? GoogleMapsUrl { get; set; }
    public string? InstagramUrl { get; set; }
    public string? PhoneNumber { get; set; }
    public ICollection<Promo> Promos { get; set; }
    public DateTimeOffset ChangedAt { get; set; }
}