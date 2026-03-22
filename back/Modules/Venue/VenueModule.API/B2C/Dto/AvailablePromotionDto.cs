namespace VenueModule.API.B2C.Dto;

public record AvailablePromotionDto(
    string CompanyName,
    string? CompanyAvatarImageUrl,
    string VenueId,
    string VenueAddress,
    string? GoogleMapsUrl,
    PromoDto[] Promos
);