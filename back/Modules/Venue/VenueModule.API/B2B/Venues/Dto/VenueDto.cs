using VenueModule.API.B2B.Deals.Dto;
using VenueModule.API.B2B.Tags.Dto;

namespace VenueModule.API.B2B.Venues.Dto;

public record VenueDto(
    string Id,
    bool IsVerified,
    double Latitude,
    double Longitude,
    string Address,
    string? WebsiteUrl,
    string? GoogleMapsUrl,
    string? InstagramUrl,
    string? PhoneNumber,
    string? WorkingHours,
    string? CoverImageUrl,
    string PinCode,
    bool IsGeneratedPinCode,
    bool IsPinCheckInAllowed,
    Guid QrCode,
    TagDto[] Tags,
    PromoDto[] Promos,
    DiscountDto[] Discounts,
    LoyaltyLevelDto[] LoyaltyLevels);