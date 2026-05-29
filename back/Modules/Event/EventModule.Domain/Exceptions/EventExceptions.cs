using System.Net;
using SharedCore.Exceptions;

namespace EventModule.Domain.Exceptions;

public static class EventExceptions
{
    public static HttpCodeException EventNotFound(Guid id) =>
        new($"Event with id '{id}' not found", HttpStatusCode.NotFound, ErrorCode.NotFound);

    public static HttpCodeException NotEventCreator() =>
        new("Only event creator can perform this action", HttpStatusCode.Forbidden, ErrorCode.NotAllowed);

    public static HttpCodeException AlreadyParticipant() =>
        new("User is already a participant of this event", HttpStatusCode.Conflict, ErrorCode.AlreadyExists);

    public static HttpCodeException NotParticipant() =>
        new("User is not a participant of this event", HttpStatusCode.Conflict, ErrorCode.DoesNotExist);

    public static HttpCodeException EventFull() =>
        new("Event has reached maximum participants", HttpStatusCode.Conflict, ErrorCode.TooManyEntities);

    public static HttpCodeException ApprovalNotSupported() =>
        new("Approval is not supported for this event type", HttpStatusCode.UnprocessableEntity, ErrorCode.ValidationError);

    public static HttpCodeException CoverRequiredForEvent() =>
        new("Cover image is required for events of type 'Event'", HttpStatusCode.UnprocessableEntity, ErrorCode.ValidationError);

    public static HttpCodeException ApprovalOnlyForMeetup() =>
        new("RequiresApproval can only be set for meetups", HttpStatusCode.UnprocessableEntity, ErrorCode.ValidationError);

    public static HttpCodeException ConcurrencyConflict() =>
        new("Event was modified by another request. Please try again.", HttpStatusCode.Conflict, ErrorCode.GeneralError);

    public static HttpCodeException VenueNotFound(Guid venueId) =>
        new($"Venue with id '{venueId}' not found", HttpStatusCode.NotFound, ErrorCode.NotFound);

    public static HttpCodeException NotVenueOwner() =>
        new("User does not own the venue", HttpStatusCode.Forbidden, ErrorCode.NotAllowed);
}
