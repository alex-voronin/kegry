namespace VenueModule.API.B2B.Companies.Dto;

public record GetCompanyResponse(CompanyDto Company, VenueListItemDto[] Venues);
