namespace VenueModule.Domain.Aggregates;

public class VenueTag
{
    public required Guid VenueId { get; init; }
    public Venue Venue { get; init; } = null!;
    public required Guid TagId { get; init; }
    public Tag Tag { get; init; } = null!;
}
