using Microsoft.AspNetCore.Diagnostics;
using PostHog;
using Serilog;
using SharedCore.Api.Dto;
using SharedCore.Exceptions;

namespace API.Web.Exceptions;

public class ExceptionHandler : IExceptionHandler
{
    private readonly IPostHogClient _postHogClient;
    private readonly IWebHostEnvironment _hostEnvironment;

    public ExceptionHandler(IPostHogClient postHogClient, IWebHostEnvironment hostEnvironment)
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
            ArgumentNullException => (StatusCodes.Status400BadRequest, ErrorCode.GeneralError),
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, ErrorCode.GeneralError),
            HttpCodeException httpCodeException => ((int)httpCodeException.HttpStatusCode, httpCodeException.ErrorCode),
            _ => (StatusCodes.Status500InternalServerError, ErrorCode.GeneralError)
        };

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/json";

        var apiError = new ApiError()
        {
            Error = exception.Message,
            Code = Enum.GetName(typeof(ErrorCode), errorCode) ?? nameof(ErrorCode.GeneralError),
        };
        var response = new ApiResult.EmptyResult(apiError);

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(response, cancellationToken);
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