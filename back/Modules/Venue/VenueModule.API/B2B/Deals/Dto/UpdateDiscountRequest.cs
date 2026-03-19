namespace VenueModule.API.B2B.Deals.Dto;

public record UpdateDiscountRequest(
    int? LoyaltyLevelId,
    int? Percent,
    DayOfWeek[]? Days);
