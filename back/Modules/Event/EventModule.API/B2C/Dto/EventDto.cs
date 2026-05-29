using EventModule.Domain.Enums;

namespace EventModule.API.B2C.Dto;

public record EventContactDto(
    Guid Id,
    ContactType Type,
    string Value,
    bool IsPublic);

public record EventDto(
    string Id,
    string? CoverImageUrl,
    string Title,
    string Description,
    DateTimeOffset StartDateTime,
    EventType Type,
    string VenueId,
    string VenueAddress,
    double VenueLatitude,
    double VenueLongitude,
    string CreatorId,
    string? CreatorFirstName,
    string? CreatorLastName,
    string? CreatorAvatarUrl,
    bool IsVisibleOnMap,
    int? MaxParticipants,
    bool RequiresApproval,
    int ParticipantsCount,
    EventContactDto[] Contacts,
    Guid[] HobbyIds,
    bool IsJoined,
    bool IsCreator);
