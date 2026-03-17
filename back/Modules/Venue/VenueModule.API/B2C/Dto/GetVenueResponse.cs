using VenueModule.API.B2B.Venues.Dto;
using VenueModule.API.Shared;

namespace VenueModule.API.B2C.Dto;

public record GetVenueResponse(CompanyDto Company, VenueDto Venue, LoyaltyLevelWithDiscountDto LoyaltyLevel);
