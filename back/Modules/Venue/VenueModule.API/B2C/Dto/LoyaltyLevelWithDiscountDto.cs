namespace VenueModule.API.B2C.Dto;

public record LoyaltyLevelWithDiscountDto(
    int UserLoyaltyLevel,
    int VisitsCount,
    int VisitsToNextLoyaltyLevel,
    int Discount);
