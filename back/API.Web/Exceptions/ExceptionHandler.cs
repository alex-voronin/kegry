using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using PostHog;
using Serilog;
using SharedCore.Exceptions;

namespace API.Web.Exceptions;

public class ExceptionHandler : IExceptionHandler
{
    private const string GenericErrorMessage = "An unexpected error occurred.";

    private readonly IPostHogClient _postHogClient;
    private readonly IWebHostEnvironment _hostEnvironment;

    public ExceptionHandler(
        IPostHogClient postHogClient,
        IWebHostEnvironment hostEnvironment)
    {
        _postHogClient = postHogClient;
        _hostEnvironment = hostEnvironment;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        Log.Error(exception, exception.Message);

        SendToPosthog(exception, httpContext);

        var (statusCode, errorCode) = exception switch
        {
            ArgumentException => (StatusCodes.Status400BadRequest, ErrorCode.ValidationError),
            FormatException => (StatusCodes.Status400BadRequest, ErrorCode.ValidationError),
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, ErrorCode.GeneralError),
            HttpCodeException httpCodeException => ((int)httpCodeException.HttpStatusCode, httpCodeException.ErrorCode),
            _ => (StatusCodes.Status500InternalServerError, ErrorCode.GeneralError)
        };

        var codeException = exception as HttpCodeException;
        var isServerError = statusCode == StatusCodes.Status500InternalServerError;

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = ReasonPhrases.GetReasonPhrase(statusCode),
            Detail = isServerError ? GenericErrorMessage : exception.Message,
            Instance = httpContext.Request.Path
        };

        problemDetails.Extensions["code"] = errorCode.Value;

        if (codeException?.Details is { } details)
        {
            foreach (var (key, value) in details)
            {
                problemDetails.Extensions[key] = value;
            }
        }

        httpContext.Response.StatusCode = statusCode;

        problemDetails.Extensions["traceId"] = Activity.Current?.Id ?? httpContext.TraceIdentifier;

        await httpContext.Response.WriteAsJsonAsync(
            problemDetails,
            options: null,
            contentType: "application/problem+json",
            cancellationToken: cancellationToken);

        return true;
    }

    private void SendToPosthog(Exception exception, HttpContext httpContext)
    {
        if (exception is UnauthorizedAccessException or HttpCodeException)
        {
            return;
        }

        try
        {
            _postHogClient.CaptureException(
                distinctId: "backend",
                exception: exception,
                properties: new Dictionary<string, object>
                {
                    ["path"] = httpContext.Request.Path.ToString(),
                    ["method"] = httpContext.Request.Method,
                    ["environment"] = _hostEnvironment.IsDevelopment() ? "dev" : "prod",
                });
        }
        catch (Exception e)
        {
            Log.Error(e, "Unable to send exception to Posthog");
        }
    }
}
