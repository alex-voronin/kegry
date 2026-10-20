using System.Net;

namespace SharedCore.Exceptions;

public static class CommonExceptions
{
    public static HttpCodeException InternalError(string message)
        => InternalError(message, ErrorCode.GeneralError);

    public static HttpCodeException InternalError(string message, ErrorCode code)
        => new(message, HttpStatusCode.InternalServerError, code);
}
