using EventModule.Domain.Enums;

namespace EventModule.API.B2C.Dto;

public record GetEventsMapRequest(
    double MinLat,
    double MaxLat,
    double MinLng,
    double MaxLng,
    EventType[]? EventTypes = null,
    DateTimeOffset? DateFrom = null,
    DateTimeOffset? DateTo = null,
    Guid[]? VenueIds = null);
