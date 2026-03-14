using System.Net;
using SharedCore.Exceptions;

namespace VenueModule.Domain.Exceptions;

public static class VenueExceptions
{
    public static HttpCodeException CompanyNotFound()
    {
        return new HttpCodeException("No companies found", HttpStatusCode.NotFound, ErrorCode.NotFound);
    }

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

    public static HttpCodeException AlreadyCheckedInRecently()
    {
        return new HttpCodeException("You've already checked in",
            HttpStatusCode.UnprocessableEntity, ErrorCode.TooManyRequests);
    }

    public static HttpCodeException DiscountNotFound(Guid id)
    {
        return new HttpCodeException($"Discount with id {id} not found", HttpStatusCode.NotFound, ErrorCode.NotFound);
    }

    public static HttpCodeException InvalidLoyaltyLevelCount()
    {
        return new HttpCodeException("LoyaltyLevels array must contain between 0 and 3 items",
            HttpStatusCode.BadRequest, ErrorCode.ValidationError);
    }

    public static HttpCodeException InvalidLoyaltyLevelId()
    {
        return new HttpCodeException("LoyaltyLevel Id must be 0, 1, or 2",
            HttpStatusCode.BadRequest, ErrorCode.ValidationError);
    }

    public static HttpCodeException InvalidLoyaltyLevelOrder()
    {
        return new HttpCodeException(
            "LoyaltyLevel VisitsUpperBound must be strictly ascending by Id (Id 0 < Id 1 < Id 2)",
            HttpStatusCode.BadRequest, ErrorCode.ValidationError);
    }
}
