namespace VenueModule.API.B2C.Dto;

public record AvailableDiscountDto(
    string CompanyName,
    string? CompanyAvatarImageUrl,
    Guid VenueId,
    string VenueAddress,
    string? GoogleMapsUrl,
    int Percent,
    int DistanceMeters);
