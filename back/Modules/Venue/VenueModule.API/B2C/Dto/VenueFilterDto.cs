namespace VenueModule.API.B2C.Dto;

public record VenueFilterDto(
    Guid[]? TagIds = null,
    string? CompanyName = null
);
