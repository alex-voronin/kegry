using Microsoft.AspNetCore.Mvc;
using EventModule.Domain.Enums;
using SharedCore.Api.Dto;

namespace EventModule.API.B2C.Dto;

public record GetEventsRequest(
    [FromQuery] double Latitude,
    [FromQuery] double Longitude,
    [FromQuery] int Offset = 0,
    [FromQuery] int Limit = 20,
    [FromQuery] EventType? Type = null,
    [FromQuery] Guid? VenueId = null,
    [FromQuery] DateTimeOffset? DateFrom = null,
    [FromQuery] DateTimeOffset? DateTo = null,
    [FromQuery] Guid[]? HobbyIds = null) : PaginatedRequest(Offset, Limit);
