namespace VenueModule.API.B2B.Discount.Dto;

public record UpdateDiscountRequest(
    int? LoyaltyLevelId,
    int? Percent,
    DayOfWeek[]? Days);
