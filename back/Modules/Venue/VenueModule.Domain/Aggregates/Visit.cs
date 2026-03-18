namespace VenueModule.Domain.Aggregates;

public class Visit
{
    public required Guid Id { get; set; }
    public required Guid UserId { get; init; }
    public required Guid VenueId { get; init; }
    public required DateTimeOffset CheckedInAt { get; init; }
}