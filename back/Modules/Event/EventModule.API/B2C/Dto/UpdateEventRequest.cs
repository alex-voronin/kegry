namespace EventModule.API.B2C.Dto;

public record UpdateEventRequest(
    string? Title,
    string? Description,
    DateTimeOffset? StartDateTime,
    bool? IsVisibleOnMap,
    int? MaxParticipants,
    bool? RequiresApproval,
    string? CoverImageUrl,
    CreateEventContactDto[]? Contacts,
    Guid[]? HobbyIds);
