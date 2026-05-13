namespace VenueModule.API.B2C.Dto;

public record VenueDto(
    string Id,
    string QrCode,
    bool IsPinCheckInAllowed,
    double Latitude,
    double Longitude,
    string Address,
    string? WebsiteUrl,
    string? GoogleMapsUrl,
    string? InstagramUrl,
    string? PhoneNumber,
    string? WorkingHours,
    string? CoverImageUrl,
    TagDto[] Tags,
    PromoDto[] Promos);
