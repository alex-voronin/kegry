namespace VenueModule.Domain.Aggregates;

// It's a materialized view for performance optimization based on Visits table 
public class UserLoyalty
{
    public required Guid UserId { get; init; }
    public required Guid CompanyId { get; init; }
    public required int VisitsCount { get; set; }
    public required int LoyaltyLevelId { get; set; }
}
