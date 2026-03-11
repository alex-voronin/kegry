namespace VenueModule.API.Dto;

public record GetCompanyResponse(CompanyDto Company, VenueDto[] Venues);
