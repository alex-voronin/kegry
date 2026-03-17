namespace VenueModule.API.B2B.Discount.Dto;

public record CreateDiscountRequest(
    int LoyaltyLevelId,
    int Percent,
    DayOfWeek[] Days);
