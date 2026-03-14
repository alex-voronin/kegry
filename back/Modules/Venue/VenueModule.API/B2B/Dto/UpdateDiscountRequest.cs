namespace VenueModule.API.B2B.Dto;

public record UpdateDiscountRequest(
    int? LoyaltyLevelId,
    int? Percent,
    DayOfWeek[]? Days);
