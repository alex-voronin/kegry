namespace EventModule.API.B2B.Dto;

using B2C.Dto;

public record B2BCreateEventRequest(
    string? CoverImageUrl,
    string Title,
    string Description,
    DateTimeOffset StartDateTime,
    Guid VenueId,
    CreateEventContactDto[]? Contacts = null,
    Guid[]? HobbyIds = null);
