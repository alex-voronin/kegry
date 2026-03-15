using System.Net;
using SharedCore.Exceptions;

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

    public static HttpCodeException InviteExpired()
    {
        return new HttpCodeException("The invite has already expired",
            HttpStatusCode.BadRequest,
            ErrorCode.ValidationError);
    }

    public static HttpCodeException UserIsNotFriend(Guid id)
    {
        return new HttpCodeException($"User {id} is not a friend",
            HttpStatusCode.Forbidden,
            ErrorCode.ValidationError);
    }

    public static HttpCodeException UserAlreadyInMeeting()
    {
        return new HttpCodeException("The user already has a meetup scheduled.",
            HttpStatusCode.Forbidden,
            ErrorCode.ValidationError);
    }
}