namespace VenueModule.API.Shared;

public record CompanyDto(
    string Id,
    string Name,
    string? Email,
    string? AvatarImageUrl);
