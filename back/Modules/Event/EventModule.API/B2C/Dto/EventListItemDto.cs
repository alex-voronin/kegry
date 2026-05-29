using EventModule.Domain.Enums;

namespace EventModule.API.B2C.Dto;

public record EventListItemDto(
    string Id,
    string? CoverImageUrl,
    string Title,
    EventType Type,
    DateTimeOffset StartDateTime,
    string VenueId,
    string VenueAddress,
    double DistanceMeters,
    int ParticipantsCount,
    bool IsJoined);
