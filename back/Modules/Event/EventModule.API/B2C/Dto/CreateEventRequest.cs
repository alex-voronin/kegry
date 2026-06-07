using EventModule.Domain.Enums;

namespace EventModule.API.B2C.Dto;

public record CreateEventContactDto(
    ContactType Type,
    string Value,
    bool IsPublic = false);

public record CreateEventRequest(
    string? CreatorAvatarUrl,
    string? CoverImageUrl,
    string Title,
    string Description,
    DateTimeOffset StartDateTime,
    Guid VenueId,
    int? MaxParticipants = null,
    bool RequiresApproval = false,
    CreateEventContactDto[]? Contacts = null,
    Guid[]? HobbyIds = null);
