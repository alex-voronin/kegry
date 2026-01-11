using System.Net;
using SharedCore.Exceptions;

namespace ChatModule.Domain.Exceptions;

public static class ChatExceptions
{
    public static HttpCodeException ChatNotFound(Guid id)
    {
        return new HttpCodeException($"Chat for user with id {id} is not found",
            HttpStatusCode.NotFound, ErrorCode.NotFound);
    }

    public static HttpCodeException ChatParticipantNotFound(Guid id)
    {
        return new HttpCodeException($"Chat participant for user with id {id} is not found",
            HttpStatusCode.InternalServerError, ErrorCode.NotFound);
    }
}