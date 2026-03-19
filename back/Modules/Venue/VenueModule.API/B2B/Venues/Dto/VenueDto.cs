using VenueModule.API.B2B.Deals.Dto;

namespace VenueModule.API.B2B.Venues.Dto;

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
    PromoDto[] Promos,
    DiscountDto[] Discounts);