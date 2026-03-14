namespace VenueModule.API.Dto;

public record GetVenueResponse(CompanyDto Company, VenueDto Venue, LoyaltyLevelWithDiscountDto LoyaltyLevel);
