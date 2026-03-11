namespace VenueModule.API.Dto;

public record PatchVenueRequest(
    double? Latitude,
    double? Longitude,
    string? Address,
    string? GoogleMapsUrl,
    string? InstagramUrl,
    string? PhoneNumber,
    string? CoverImageUrl,
    PromoDto[]? Promos);
