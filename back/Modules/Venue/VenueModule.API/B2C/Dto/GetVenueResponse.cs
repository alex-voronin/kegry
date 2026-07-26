using VenueModule.API.Shared;

namespace VenueModule.API.B2C.Dto;

public record GetVenueResponse(CompanyDto Company, VenueDto Venue, LoyaltyLevelWithDiscountDto? LoyaltyLevel);
