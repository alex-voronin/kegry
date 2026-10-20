using System.Net;

namespace SharedCore.Exceptions;

public class HttpCodeException : Exception
{
    public HttpStatusCode HttpStatusCode { get; init; }

    /// <summary>
    /// Client-facing error code, serialized as the <c>code</c> extension of ProblemDetails.
    /// </summary>
    public ErrorCode ErrorCode { get; init; }

    /// <summary>
    /// Additional machine-readable context merged into ProblemDetails extensions (e.g. <c>venueId</c>).
    /// Do not use the keys reserved by ProblemDetails (<c>code</c>, <c>traceId</c>, <c>type</c>,
    /// <c>title</c>, <c>status</c>, <c>detail</c>, <c>instance</c>): they are written by the exception handler.
    /// </summary>
    public IReadOnlyDictionary<string, object?>? Details { get; init; }

    public HttpCodeException(string message, HttpStatusCode httpStatusCode, ErrorCode errorCode) : base(message)
    {
        HttpStatusCode = httpStatusCode;
        ErrorCode = errorCode;
    }
}
