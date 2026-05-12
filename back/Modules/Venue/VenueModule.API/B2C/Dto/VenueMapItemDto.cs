namespace VenueModule.API.B2C.Dto;

public record VenueMapItemDto(
    string Id,
    string CompanyName,
    string? CompanyAvatarImageUrl,
    string Address,
    double Latitude,
    double Longitude);
