namespace EventModule.API.B2C.Dto;

public record VenueEventsDto(
    string VenueId,
    int TotalCount,
    EventMapItemDto[] Events);
