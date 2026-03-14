namespace VenueModule.API.B2C.Dto;

public record VenueListItemDto(
    string Id,
    string CompanyAvatarImageUrl,
    string CompanyName,
    string Address,
    int DistanceMeters,
    string? GoogleMapsUrl
);