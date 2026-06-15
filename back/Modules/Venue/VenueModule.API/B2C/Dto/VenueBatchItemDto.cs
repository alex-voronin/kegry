namespace VenueModule.API.B2C.Dto;

public record VenueBatchItemDto(
    string Id,
    string Name,
    string? AvatarUrl,
    string Address);
