using VenueModule.API.Shared;

namespace VenueModule.API.B2B.Dto;

public record GetCompanyResponse(CompanyDto Company, VenueDto[] Venues);
