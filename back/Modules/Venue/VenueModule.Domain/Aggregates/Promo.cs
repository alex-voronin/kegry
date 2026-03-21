using VenueModule.Domain.ValueObjects;

namespace VenueModule.Domain.Aggregates;

public class Promo
{
    public required Guid Id { get; init; }
    public required Guid VenueId { get; init; }
    public Venue Venue { get; init; } = null!;
    public required PromoType Type { get; set; }
    public required string Body { get; set; } = null!;
}