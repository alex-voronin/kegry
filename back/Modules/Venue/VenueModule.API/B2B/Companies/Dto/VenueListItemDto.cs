namespace VenueModule.API.B2B.Companies.Dto;

public record VenueListItemDto(
    Guid Id,
    bool IsVerified,
    string? CoverImageUrl,
    string Address,
    string PinCode);
