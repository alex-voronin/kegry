namespace VenueModule.API.Dto;

public record UpdateDiscountRequest(
    int? LoyaltyLevelId,
    int? Percent,
    DayOfWeek[]? Days);
