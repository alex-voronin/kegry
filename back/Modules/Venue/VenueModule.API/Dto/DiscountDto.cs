namespace VenueModule.API.Dto;

public record DiscountDto(
    string Id,
    string VenueId,
    int? LoyaltyLevelId,
    int Percent,
    DayOfWeek[] Days);
