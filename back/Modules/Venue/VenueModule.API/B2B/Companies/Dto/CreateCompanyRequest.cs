namespace VenueModule.API.B2B.Companies.Dto;

public record CreateCompanyRequest(
    string Name,
    string? Email);
