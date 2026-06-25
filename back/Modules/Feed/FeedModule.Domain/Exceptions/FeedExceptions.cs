using System.Net;
using Microsoft.Extensions.Localization;
using SharedCore.Exceptions;
using FeedModule.Domain.Resources;

namespace FeedModule.Domain.Exceptions;

public static class FeedExceptions
{
    public static HttpCodeException UserLocationIsUnknown(Guid id)
    {
        return new HttpCodeException($"User location is unknown {id}",
            HttpStatusCode.UnprocessableEntity,
            ErrorCode.NotFound);
    }

    public static HttpCodeException UserProfileIsNotFound(Guid id)
    {
        return new HttpCodeException($"User profile is not found {id}",
            HttpStatusCode.UnprocessableEntity,
            ErrorCode.NotFound);
    }

    public static HttpCodeException NoAvailableInvitesLeft(Guid id)
    {
        return new HttpCodeException($"No available invites left for user {id}",
            HttpStatusCode.TooManyRequests,
            ErrorCode.TooManyRequests);
    }

    public static HttpCodeException InviteExpirationTooFar()
    {
        return new HttpCodeException("Invite expiration must not exceed 48 hours from now",
            HttpStatusCode.BadRequest,
            ErrorCode.ValidationError);
    }

    public static HttpCodeException InviteExpired(IStringLocalizer<FeedLocalization> localizer)
    {
        return new HttpCodeException(localizer.GetString("InviteExpired"),
            HttpStatusCode.BadRequest,
            ErrorCode.ValidationError);
    }

    public static HttpCodeException UserIsNotFriend(Guid id)
    {
        return new HttpCodeException($"User {id} is not a friend",
            HttpStatusCode.Forbidden,
            ErrorCode.ValidationError);
    }

    public static HttpCodeException OtherUserAlreadyInMeeting(IStringLocalizer<FeedLocalization> localizer)
    {
        return new HttpCodeException(localizer.GetString("OtherUserAlreadyInMeeting"),
            HttpStatusCode.Forbidden,
            ErrorCode.ValidationError);
    }

    public static HttpCodeException YouAlreadyInMeeting(IStringLocalizer<FeedLocalization> localizer)
    {
        return new HttpCodeException(localizer.GetString("YouAlreadyInMeeting"),
            HttpStatusCode.Forbidden,
            ErrorCode.ValidationError);
    }
}