namespace VenueModule.API.B2B.Dto;

public record CreateDiscountRequest(
    int LoyaltyLevelId,
    int Percent,
    DayOfWeek[] Days);
