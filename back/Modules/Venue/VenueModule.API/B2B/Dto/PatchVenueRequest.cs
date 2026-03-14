using VenueModule.API.Shared;

namespace VenueModule.API.B2B.Dto;

public record PatchVenueRequest(
    double? Latitude,
    double? Longitude,
    string? Address,
    string? WebsiteUrl,
    string? GoogleMapsUrl,
    string? InstagramUrl,
    string? PhoneNumber,
    string? WorkingHours,
    string? CoverImageUrl,
    PromoDto[]? Promos);