namespace VenueModule.Domain.Aggregates;

public class Visit
{
    public Guid UserId { get; init; }
    public Guid VenueId { get; init; }
    public DateTimeOffset CheckedInAt { get; init; }
}