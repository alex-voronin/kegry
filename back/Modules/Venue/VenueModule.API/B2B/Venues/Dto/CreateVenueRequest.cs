namespace VenueModule.API.B2B.Venues.Dto;

public record CreateVenueRequest(
    double Latitude,
    double Longitude,
    string Address,
    string? WebsiteUrl,
    string? GoogleMapsUrl,
    string? InstagramUrl,
    string? PhoneNumber,
    string? WorkingHours,
    string? CoverImageUrl,
    Guid[] TagIds);