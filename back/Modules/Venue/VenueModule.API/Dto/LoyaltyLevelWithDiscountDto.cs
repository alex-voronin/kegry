namespace VenueModule.API.Dto;

public record LoyaltyLevelWithDiscountDto(
    int UserLoyaltyLevel,
    int VisitsCount,
    int VisitsToNextLoyaltyLevel,
    int Discount);
