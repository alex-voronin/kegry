namespace EventModule.API.B2B.Dto;

using B2C.Dto;

public record B2BCreateEventRequest(
    string? CoverImageUrl,
    string Title,
    string Description,
    DateTimeOffset StartDateTime,
    Domain.Enums.EventType Type,
    Guid VenueId,
    int? MaxParticipants = null,
    bool RequiresApproval = false,
    CreateEventContactDto[]? Contacts = null,
    Guid[]? HobbyIds = null);
