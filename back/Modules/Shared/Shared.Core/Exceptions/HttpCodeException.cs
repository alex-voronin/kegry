using System.Net;

namespace SharedCore.Exceptions;

public class HttpCodeException : Exception
{
    public HttpStatusCode HttpStatusCode { get; init; }
    public ErrorCode ErrorCode { get; init; }
    public string? Code { get; init; }
    public IReadOnlyDictionary<string, object?>? Details { get; init; }

    public HttpCodeException(string message, HttpStatusCode httpStatusCode, ErrorCode errorCode) : base(message)
    {
        HttpStatusCode = httpStatusCode;
        ErrorCode = errorCode;
    }
}