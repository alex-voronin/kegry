using System.Net;

namespace SharedCore.Exceptions;

public class CommonExceptions
{
    public static HttpCodeException InternalError(string message, ErrorCode code = ErrorCode.GeneralError)
    {
        return new HttpCodeException(message, HttpStatusCode.InternalServerError, code);
    }
}