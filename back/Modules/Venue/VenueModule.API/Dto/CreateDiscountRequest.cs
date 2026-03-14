namespace VenueModule.API.Dto;

public record CreateDiscountRequest(
    int? LoyaltyLevelId,
    int Percent,
    DayOfWeek[] Days);
