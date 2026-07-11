using EventModule.Domain.Enums;

namespace EventModule.API.B2C.Dto;

public record EventListItemDto(
    string Id,
    string VenueId,
    string? CoverImageUrl,
    string Title,
    EventType Type,
    DateTimeOffset StartDateTime,
    int ParticipantsCount,
    int? MaxParticipants,
    string? ParticipationStatus,
    string CreatorId,
    string? CreatorAvatarUrl);
