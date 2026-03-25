using SharedCore.Validation;
using VenueModule.Domain.Exceptions;

namespace VenueModule.Domain.Aggregates;

public class LoyaltyLevel
{
    public required int Id
    {
        get;
        init => field = ValidateId(value);
    }

    public required Guid CompanyId { get; init; }

    public required int VisitsUpperBound
    {
        get;
        set => field = ValidateUpperBound(value);
    }

    public required bool IsActive { get; set; }

    // ── Validation ────────────────────────────────────────────────────────────

    public static void ValidateLevelsCountAndOrder(IReadOnlyCollection<LoyaltyLevel> levels)
    {
        if (levels.Count > 4)
        {
            throw VenueExceptions.InvalidLoyaltyLevelCount();
        }

        var sorted = levels.OrderBy(l => l.Id).ToArray();
        for (var i = 1; i < sorted.Length; i++)
        {
            if (sorted[i].VisitsUpperBound <= sorted[i - 1].VisitsUpperBound &&
                sorted[i].IsActive && sorted[i - 1].IsActive)
            {
                throw VenueExceptions.InvalidLoyaltyLevelOrder();
            }

            if (sorted[i].IsActive && !sorted[i - 1].IsActive)
            {
                throw VenueExceptions.GapsInLoyaltyLevelOrder();
            }
        }
    }

    private static int ValidateId(int value)
    {
        Validation.ThrowIfInvalid(nameof(Id), () => value >= 0 && value <= 3, "LoyaltyLevel Id must be [0..3]");
        return value;
    }

    private static int ValidateUpperBound(int value)
    {
        Validation.ThrowIfInvalid(nameof(Id), () => value >= 0, "UpperBound must be >= 0");
        return value;
    }
}