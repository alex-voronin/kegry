namespace VenueModule.API.Shared;

public record VenueDto(
    string Id,
    double Latitude,
    double Longitude,
    string Address,
    string? WebsiteUrl,
    string? GoogleMapsUrl,
    string? InstagramUrl,
    string? PhoneNumber,
    string? WorkingHours,
    string? CoverImageUrl,
    PromoDto[] Promos);
