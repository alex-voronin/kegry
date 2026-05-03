using VenueModule.API.B2B.Venues.Dto;

namespace VenueModule.API.B2C.Dto;

public record VenueDto(
    string Id,
    string QrCode,
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
