using Microsoft.AspNetCore.Mvc;
using EventModule.Domain.Enums;
using SharedCore.Api.Dto;

namespace EventModule.API.B2B.Dto;

public record B2BGetEventsRequest(
    [FromQuery] int Offset = 0,
    [FromQuery] int Limit = 20,
    [FromQuery] Guid? VenueId = null,
    [FromQuery] EventType? Type = null,
    [FromQuery] DateTimeOffset? DateFrom = null,
    [FromQuery] DateTimeOffset? DateTo = null) : PaginatedRequest(Offset, Limit);
