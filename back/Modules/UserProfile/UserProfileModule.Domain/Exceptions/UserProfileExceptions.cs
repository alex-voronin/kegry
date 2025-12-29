using System.Net;
using SharedCore.Exceptions;

namespace UserProfileModule.Domain.Exceptions;

public static class UserProfileExceptions
{
    public static HttpCodeException ProfileNotFound(Guid id)
    {
        return new HttpCodeException($"Profile with id {id} not found", HttpStatusCode.NotFound, ErrorCode.NotFound);
    }
}