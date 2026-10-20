using System.Net;
using Microsoft.Extensions.Localization;
using SharedCore.Exceptions;
using VenueModule.Domain.Resources;

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

    public static HttpCodeException VenueNotFoundByQr(Guid qrCode)
    {
        return new HttpCodeException($"Venue with QR-Code {qrCode} not found", HttpStatusCode.NotFound, ErrorCode.NotFound)
        {
            Code = "VenueNotFound"
        };
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

    public static HttpCodeException CheckInPinDisabled(
        Guid venueId, IStringLocalizer<VenueLocalization> localizer)
        => CheckInFailure(
            venueId,
            localizer.GetString("PinCheckInDisabled"),
            HttpStatusCode.Forbidden,
            ErrorCode.NotAllowed,
            "PinCheckInDisabled");

    public static HttpCodeException CheckInInvalidPin(
        Guid venueId, IStringLocalizer<VenueLocalization> localizer)
        => CheckInFailure(
            venueId,
            localizer.GetString("InvalidPinCodeError"),
            HttpStatusCode.UnprocessableEntity,
            ErrorCode.ValidationError,
            "InvalidPinCode");

    public static HttpCodeException CheckInOutsideRadius(
        Guid venueId, IStringLocalizer<VenueLocalization> localizer)
        => CheckInFailure(
            venueId,
            localizer.GetString("NotInVenueError"),
            HttpStatusCode.UnprocessableEntity,
            ErrorCode.ValidationError,
            "OutsideVenueRadius");

    public static HttpCodeException CheckInAlreadyCheckedIn(
        Guid venueId, IStringLocalizer<VenueLocalization> localizer)
        => CheckInFailure(
            venueId,
            localizer.GetString("AlreadyCheckedInError"),
            HttpStatusCode.TooManyRequests,
            ErrorCode.TooManyRequests,
            "AlreadyCheckedIn");

    public static HttpCodeException CheckInCoordinatesRequired()
        => new("Either PIN or coordinates must be provided",
            HttpStatusCode.UnprocessableEntity,
            ErrorCode.ValidationError)
        {
            Code = "CoordinatesRequired"
        };

    private static HttpCodeException CheckInFailure(
        Guid venueId,
        string message,
        HttpStatusCode statusCode,
        ErrorCode errorCode,
        string code)
    {
        return new HttpCodeException(message, statusCode, errorCode)
        {
            Code = code,
            Details = new Dictionary<string, object?>
            {
                ["venueId"] = venueId
            }
        };
    }
}