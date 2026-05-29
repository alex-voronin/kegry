namespace EventModule.API.B2B.Dto;

using B2C.Dto;

public record B2BUpdateEventRequest(
    string? Title,
    string? Description,
    DateTimeOffset? StartDateTime,
    string? CoverImageUrl,
    CreateEventContactDto[]? Contacts,
    Guid[]? HobbyIds);
