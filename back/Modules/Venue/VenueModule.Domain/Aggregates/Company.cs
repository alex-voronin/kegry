using VenueModule.Domain.ValueObjects;

namespace VenueModule.Domain.Aggregates;

public class Company
{
    public Guid Id { get; init; }
    public Guid OwnerId { get; init; }
    public string Name { get; set; } = null!;
    public string AvatarImageS3Key { get; set; } = null!;
    public string? Email { get; set; }
    public DateTimeOffset ChangedAt { get; set; }
    public ICollection<Venue> Venues { get; set; }
}