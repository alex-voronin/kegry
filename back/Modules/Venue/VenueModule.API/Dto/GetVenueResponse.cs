namespace VenueModule.API.Dto;

public record GetVenueResponse(CompanyDto Company, VenueDto Venue, int LoyaltyLevel);
