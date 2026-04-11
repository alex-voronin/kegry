using System.Net;
using SharedCore.Exceptions;

namespace VenueModule.Domain.Exceptions;

public static class VenueExceptions
{
    public static HttpCodeException CompanyNotFound()
    {
        return new HttpCodeException("No companies found", HttpStatusCode.NotFound, ErrorCode.NotFound);
    }

    public static HttpCodeException TooManyCompanies()
    {
        return new HttpCodeException("You've reached the maximum number of companies.",
            HttpStatusCode.Forbidden, ErrorCode.TooManyEntities);
    }

    public static HttpCodeException VenueNotFound(Guid id)
    {
        return new HttpCodeException($"Venue with id {id} not found", HttpStatusCode.NotFound, ErrorCode.NotFound);
    }

    public static HttpCodeException TooManyVenues(string companyName)
    {
        return new HttpCodeException($"You've reached the maximum number of venues for {companyName}.",
            HttpStatusCode.Forbidden, ErrorCode.TooManyEntities);
    }

    public static HttpCodeException SomeoneElseCompany(Guid id)
    {
        return new HttpCodeException($"Company with id {id} doesn't belong to you",
            HttpStatusCode.NotFound, ErrorCode.NotFound);
    }

    public static HttpCodeException DiscountNotFound(Guid id)
    {
        return new HttpCodeException($"Discount with id {id} not found", HttpStatusCode.NotFound, ErrorCode.NotFound);
    }

    public static HttpCodeException PromoNotFound(Guid id)
    {
        return new HttpCodeException($"Promotion with id {id} not found", HttpStatusCode.NotFound, ErrorCode.NotFound);
    }

    public static HttpCodeException InvalidLoyaltyLevelCount()
    {
        return new HttpCodeException("LoyaltyLevels array must contain between 0 and 3 items",
            HttpStatusCode.BadRequest, ErrorCode.ValidationError);
    }

    public static HttpCodeException InvalidLoyaltyLevelOrder()
    {
        return new HttpCodeException(
            "Loyalty levels must be strictly ascending by visits count",
            HttpStatusCode.BadRequest, ErrorCode.ValidationError);
    }

    public static HttpCodeException GapsInLoyaltyLevelOrder()
    {
        return new HttpCodeException(
            "Loyalty levels can't have a disabled level between active ones.",
            HttpStatusCode.BadRequest, ErrorCode.ValidationError);
    }
}