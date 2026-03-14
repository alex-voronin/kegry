namespace VenueModule.Domain.Aggregates;

public class Discount
{
    public Guid Id { get; init; }
    public Guid VenueId { get; init; }
    public int? LoyaltyLevelId { get; set; }
    public LoyaltyLevel? LoyaltyLevel { get; init; }
    public int Percent { get; set; }
    public bool ActiveBySunday { get; set; }
    public bool ActiveByMonday { get; set; }
    public bool ActiveByTuesday { get; set; }
    public bool ActiveByWednesday { get; set; }
    public bool ActiveByThursday { get; set; }
    public bool ActiveByFriday { get; set; }
    public bool ActiveBySaturday { get; set; }
}