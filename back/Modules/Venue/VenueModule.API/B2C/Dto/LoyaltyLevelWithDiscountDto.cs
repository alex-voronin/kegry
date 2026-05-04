namespace VenueModule.API.B2C.Dto;

public record LoyaltyLevelWithDiscountDto(
    int UserLoyaltyLevel,
    int VisitsCount,
    int VisitsLowerBound,
    int? VisitsUpperBound,
    int? Discount,
    int? NextLevelDiscount,
    bool IsCustomDiscount);
