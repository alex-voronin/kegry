using Microsoft.AspNetCore.Mvc;
using EventModule.Domain.Enums;

namespace EventModule.API.B2C.Dto;

public record GetEventsMapRequest(
    [FromQuery] double MinLat,
    [FromQuery] double MaxLat,
    [FromQuery] double MinLng,
    [FromQuery] double MaxLng,
    [FromQuery] EventType? Type = null,
    [FromQuery] DateTimeOffset? DateFrom = null,
    [FromQuery] DateTimeOffset? DateTo = null);
