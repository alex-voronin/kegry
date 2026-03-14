using SharedCore.Validation;
using VenueModule.Domain.Exceptions;

namespace VenueModule.Domain.Aggregates;

public class LoyaltyLevel
{
    public int Id
    {
        get;
        init => field = ValidateId(value);
    }

    public Guid VenueId { get; init; }

    public int VisitsUpperBound
    {
        get;
        set => field = ValidateVisitsUpperBound(value);
    }

    // ── Validation ────────────────────────────────────────────────────────────

    /// <summary>
    /// Validates that a collection of loyalty levels has 0–3 items,
    /// all Ids are in {0,1,2}, and VisitsUpperBound is strictly ascending by Id.
    /// </summary>
    public static void ValidateLevelsCountAndOrder(IReadOnlyCollection<LoyaltyLevel> levels)
    {
        if (levels.Count > 3)
            throw VenueExceptions.InvalidLoyaltyLevelCount();

        var sorted = levels.OrderBy(l => l.Id).ToArray();
        for (var i = 1; i < sorted.Length; i++)
        {
            if (sorted[i].VisitsUpperBound <= sorted[i - 1].VisitsUpperBound)
                throw VenueExceptions.InvalidLoyaltyLevelOrder();
        }
    }

    private static int ValidateId(int value)
    {
        Validation.ThrowIfInvalid(nameof(Id), () => value >= 0 && value <= 2, "LoyaltyLevel Id must be 0, 1, or 2");
        return value;
    }

    private static int ValidateVisitsUpperBound(int value)
    {
        Validation.ThrowIfInvalid(nameof(VisitsUpperBound), () => value > 0, "VisitsUpperBound must be greater than 0");
        return value;
    }
}