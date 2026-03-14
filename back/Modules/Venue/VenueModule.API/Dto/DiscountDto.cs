namespace VenueModule.API.Dto;

public record DiscountDto(
    string Id,
    string VenueId,
    int? LoyaltyLevelId,
    int Percent,
    bool ActiveBySunday,
    bool ActiveByMonday,
    bool ActiveByTuesday,
    bool ActiveByWednesday,
    bool ActiveByThursday,
    bool ActiveByFriday,
    bool ActiveBySaturday);
