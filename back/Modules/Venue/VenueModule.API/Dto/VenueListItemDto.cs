namespace VenueModule.API.Dto;

public record VenueListItemDto(
    string Id,
    string CompanyAvatarImageUrl,
    string CompanyName,
    string Address
);