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
}