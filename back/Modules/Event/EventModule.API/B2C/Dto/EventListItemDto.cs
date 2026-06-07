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
    bool IsJoined,
    string CreatorId,
    string? CreatorAvatarUrl);
