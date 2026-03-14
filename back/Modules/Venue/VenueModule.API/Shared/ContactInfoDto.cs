namespace VenueModule.API.Shared;

public record ContactInfoDto(
    double Latitude,
    double Longitude,
    string Address,
    string? GoogleMapsUrl,
    string? InstagramUrl,
    string? Email,
    string? PhoneNumber);
