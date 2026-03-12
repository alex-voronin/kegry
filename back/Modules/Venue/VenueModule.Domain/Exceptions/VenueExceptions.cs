using System.Net;
using SharedCore.Exceptions;

namespace VenueModule.Domain.Exceptions;

public static class VenueExceptions
{
    public static HttpCodeException VenueNotFound(Guid id)
    {
        return new HttpCodeException($"Venue with id {id} not found", HttpStatusCode.NotFound, ErrorCode.NotFound);
    }

    public static HttpCodeException TooFarFromVenue()
    {
        return new HttpCodeException("You must be within 100 meters of the venue to check in",
            HttpStatusCode.BadRequest, ErrorCode.ValidationError);
    }

    public static HttpCodeException SomeoneElseCompany(Guid id)
    {
        return new HttpCodeException($"Company with id {id} doesn't belong to you",
            HttpStatusCode.NotFound, ErrorCode.NotFound);
    }
}
