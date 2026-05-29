using EventModule.Domain.Enums;

namespace EventModule.API.B2B.Dto;

public record B2BEventListItemDto(
    string Id,
    string? CoverImageUrl,
    string Title,
    EventType Type,
    DateTimeOffset StartDateTime,
    string VenueId,
    string VenueAddress,
    bool IsVisibleOnMap,
    int? MaxParticipants,
    bool RequiresApproval,
    int ParticipantsCount);
