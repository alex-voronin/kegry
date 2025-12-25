using Microsoft.AspNetCore.Diagnostics;
using Serilog;
using SharedCore.Api.Dto;
using SharedCore.Exceptions;

namespace API.Web.Exceptions;

public class ExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        Log.Error(exception, exception.Message);
        var (statusCode, errorCode) = exception switch
        {
            ArgumentNullException => (StatusCodes.Status400BadRequest, ErrorCode.GeneralError),
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, ErrorCode.GeneralError),
            HttpCodeException httpCodeException => ((int)httpCodeException.HttpStatusCode, ErrorCode.GeneralError),
            _ => (StatusCodes.Status500InternalServerError, ErrorCode.GeneralError)
        };

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/json";

        var apiError = new ApiError()
        {
            Error = exception.Message,
            Code = Enum.GetName(typeof(ErrorCode), errorCode) ?? nameof(ErrorCode.GeneralError),
        };
        var response = new EmptyApiResult(apiError);

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(response, cancellationToken);
        return true;
    }
}