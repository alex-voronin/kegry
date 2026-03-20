using SharedCore.Validation;

namespace VenueModule.Domain.Aggregates;

public class Discount
{
    public required Guid Id { get; init; }
    public required Guid VenueId { get; init; }
    public Venue Venue { get; init; } = null!;
    public required Guid CompanyId { get; init; }
    public required int LoyaltyLevelId { get; set; }
    public LoyaltyLevel LoyaltyLevel { get; init; } = null!;

    public required int Percent
    {
        get;
        set => field = ValidatePercent(nameof(Percent), value);
    }

    public ICollection<DiscountDay> Days { get; set; } = [];

    private static int ValidatePercent(string fieldName, int value)
    {
        Validation.ThrowIfNotInRange(fieldName, value, 1, 100);
        return value;
    }
}