namespace EventModule.API.B2B.Dto;

using B2C.Dto;

public record B2BUpdateEventRequest(
    string? Title,
    string? Description,
    DateTimeOffset? StartDateTime,
    Domain.Enums.EventType? Type,
    int? MaxParticipants,
    bool? RequiresApproval,
    string? CoverImageUrl,
    CreateEventContactDto[]? Contacts,
    Guid[]? HobbyIds);
