using FeedModule.Domain.ValueObjects;

namespace FeedModule.API.Dto;

public record SettingsDto(DistanceUnits DistanceUnits);

public record GetSettingsResponse(SettingsDto Settings);