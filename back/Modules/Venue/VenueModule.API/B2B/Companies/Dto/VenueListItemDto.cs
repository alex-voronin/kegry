namespace VenueModule.API.B2B.Companies.Dto;

public record VenueListItemDto(
    string Id,
    bool IsVerified,
    string? CoverImageUrl,
    string Address);
