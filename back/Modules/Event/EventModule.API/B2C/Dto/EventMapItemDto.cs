using EventModule.Domain.Enums;

namespace EventModule.API.B2C.Dto;

public record EventMapItemDto(
    string Id,
    string? CoverImageUrl,
    string Title,
    EventType Type,
    DateTimeOffset StartDateTime,
    string VenueId,
    int ParticipantsCount,
    int? MaxParticipants,
    string? ParticipationStatus,
    string CreatorId,
    string? CreatorAvatarUrl);
