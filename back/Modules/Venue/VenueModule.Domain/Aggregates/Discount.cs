using SharedCore.Interfaces;
using SharedCore.Validation;

namespace VenueModule.Domain.Aggregates;

public class Discount : IAuditable
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

    private readonly List<DiscountDay> _days = [];

    public IReadOnlyCollection<DiscountDay> Days
    {
        get => _days;
        set => UpdateDays(value);
    }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    private void UpdateDays(IReadOnlyCollection<DiscountDay> newDays)
    {
        var uniqueDaysCount = newDays.Select(d => d.DayOfWeek).Distinct().Count();
        Validation.ThrowIfInvalid(nameof(Days),
            isValid: () => uniqueDaysCount == newDays.Count,
            validationError: "Days cannot be repeated");

        var newDaysOfWeek = newDays.Select(d => d.DayOfWeek).ToHashSet();

        var daysToRemove = _days
            .Where(d => !newDaysOfWeek.Contains(d.DayOfWeek))
            .ToList();

        foreach (var day in daysToRemove)
        {
            _days.Remove(day);
        }

        var existingDayOfWeeks = _days.Select(d => d.DayOfWeek).ToHashSet();
        foreach (var day in newDays)
        {
            if (!existingDayOfWeeks.Contains(day.DayOfWeek))
            {
                _days.Add(
                    new DiscountDay
                    {
                        DiscountId = Id,
                        DayOfWeek = day.DayOfWeek
                    }
                );
            }
        }
    }

    private static int ValidatePercent(string fieldName, int value)
    {
        Validation.ThrowIfNotInRange(fieldName, value, 1, 100);
        return value;
    }
}