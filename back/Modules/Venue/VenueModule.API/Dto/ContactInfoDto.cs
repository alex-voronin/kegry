namespace VenueModule.API.Dto;

public record ContactInfoDto(
    double Latitude,
    double Longitude,
    string Address,
    string? GoogleMapsUrl,
    string? InstagramUrl,
    string? Email,
    string? PhoneNumber);
