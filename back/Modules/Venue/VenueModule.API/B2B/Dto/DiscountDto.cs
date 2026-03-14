namespace VenueModule.API.B2B.Dto;

public record DiscountDto(
    string Id,
    string VenueId,
    int? LoyaltyLevelId,
    int Percent,
    DayOfWeek[] Days);
