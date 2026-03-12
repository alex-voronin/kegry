namespace VenueModule.API.Dto;

public record VenueDto(
    string Id,
    double Latitude,
    double Longitude,
    string Address,
    string? GoogleMapsUrl,
    string? InstagramUrl,
    string? PhoneNumber,
    string? CoverImageUrl,
    PromoDto[] Promos);
