using System.Net;
using SharedCore.Exceptions;

namespace SecurityModule.Domain.Exceptions;

public static class SecurityExceptions
{
    public static HttpCodeException UnauthorizedAccess(string? message = null)
    {
        var exMessage = message ?? "Unauthorized access";
        return new HttpCodeException(exMessage, HttpStatusCode.Forbidden, ErrorCode.NotFound);
    }
    
    public static HttpCodeException UserNotFound(Guid id)
    {
        return new HttpCodeException($"User with id {id} not found", HttpStatusCode.NotFound, ErrorCode.NotFound);
    }
    
    public static HttpCodeException UserNotFound(string email)
    {
        return new HttpCodeException($"User with email {email} not found", HttpStatusCode.NotFound, ErrorCode.NotFound);
    }
    
    public static HttpCodeException UserAlreadyExists(string email)
    {
        return new HttpCodeException($"User with email {email} already exists", HttpStatusCode.UnprocessableEntity, ErrorCode.AlreadyExists);
    }
    
    public static HttpCodeException AccessTokenAlreadyExists(string email)
    {
        return new HttpCodeException($"Access token already exists for {email}", HttpStatusCode.UnprocessableEntity, ErrorCode.AlreadyExists);
    }
    
    public static HttpCodeException AccessTokensLimitPerUserIsReached(string email)
    {
        return new HttpCodeException($"Access tokens limit is reached for {email}", HttpStatusCode.UnprocessableEntity, ErrorCode.TooManyEntities);
    }
    
    public static HttpCodeException InternalError(string description)
    {
        return new HttpCodeException(description, HttpStatusCode.InternalServerError, ErrorCode.GeneralError);
    }
}