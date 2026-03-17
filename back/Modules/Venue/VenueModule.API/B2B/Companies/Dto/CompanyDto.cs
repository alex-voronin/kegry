namespace VenueModule.API.B2B.Companies.Dto;

public record CompanyDto(
    string Id,
    string Name,
    string? Email,
    string? AvatarImageUrl);
