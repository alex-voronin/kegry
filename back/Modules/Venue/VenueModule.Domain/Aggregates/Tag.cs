using VenueModule.Domain.ValueObjects;

namespace VenueModule.Domain.Aggregates;

public class Tag
{
    public required Guid Id { get; init; }
    public required string Value { get; init; }
    public required TagCategory Category { get; init; }
    public Guid? ParentId { get; init; }
    public Tag? Parent { get; init; }
    public bool IsSingleSelect { get; init; }
    public int SortOrder { get; init; }
}
