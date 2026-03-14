namespace VenueModule.Domain.Aggregates;

public class DiscountDay
{
    public Guid DiscountId { get; init; }
    public DayOfWeek DayOfWeek { get; init; }
}
