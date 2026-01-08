using System.Net;
using SharedCore.Exceptions;

namespace UserProfileModule.Domain.Exceptions;

public static class UserProfileExceptions
{
    public static HttpCodeException ProfileNotFound(Guid id)
    {
        return new HttpCodeException($"Profile with id {id} not found", HttpStatusCode.NotFound, ErrorCode.NotFound);
    }
    
    public static HttpCodeException InvalidHobbies(IReadOnlyCollection<Guid> hobbies)
    {
        return new HttpCodeException($"Invalid hobby ids: {string.Join(", ",hobbies)}",
            HttpStatusCode.NotFound, 
            ErrorCode.NotFound);
    }
}