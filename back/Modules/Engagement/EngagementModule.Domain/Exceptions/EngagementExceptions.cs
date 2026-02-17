using System.Net;
using SharedCore.Exceptions;

namespace EngagementModule.Domain.Exceptions;

public class EngagementExceptions
{
    public static HttpCodeException UserNotFound(Guid id)
    {
        return new HttpCodeException($"User is not found {id}",
            HttpStatusCode.UnprocessableEntity,
            ErrorCode.NotFound);
    }

    public static HttpCodeException NpcNotFound(Guid id)
    {
        return new HttpCodeException($"Npc is not found {id}",
            HttpStatusCode.UnprocessableEntity,
            ErrorCode.NotFound);
    }

}