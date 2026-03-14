namespace VenueModule.Domain.Aggregates;

public class Discount
{
    public Guid Id { get; init; }
    public Guid VenueId { get; init; }
    public Venue Venue { get; init; } = null!;
    public int LoyaltyLevelId { get; set; }
    public LoyaltyLevel LoyaltyLevel { get; init; } = null!;
    public int Percent { get; set; }
    public ICollection<DiscountDay> Days { get; set; } = [];
}