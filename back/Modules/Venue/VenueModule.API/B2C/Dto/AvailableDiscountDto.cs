namespace VenueModule.API.B2C.Dto;

public record AvailableDiscountDto(
    string CompanyName,
    string CompanyAvatarImageUrl,
    string VenueId,
    string VenueAddress,
    int Percent);
