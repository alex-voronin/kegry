namespace VenueModule.API.Dto;

public record CompanyDto(
    string Id,
    string Name,
    string? Email,
    string? AvatarImageUrl,
    VenueDto[] Venues);
