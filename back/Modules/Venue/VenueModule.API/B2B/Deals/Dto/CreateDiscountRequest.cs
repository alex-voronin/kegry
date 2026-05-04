namespace VenueModule.API.B2B.Deals.Dto;

public record CreateDiscountRequest(
    int LoyaltyLevelId,
    int? Percent,
    DayOfWeek[] Days);
