using System.Net;
using System.Text.Json;
using API.Web.Exceptions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using PostHog;
using SharedCore.Exceptions;
using Shouldly;

namespace Tests.API.Web;

public sealed class ExceptionHandlerTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly ErrorCode CustomErrorCode = new(nameof(CustomErrorCode));

    private readonly ExceptionHandler _handler;

    public ExceptionHandlerTests()
    {
        var hostEnvironment = Substitute.For<IWebHostEnvironment>();
        hostEnvironment.EnvironmentName = "Development";

        _handler = new ExceptionHandler(
            Substitute.For<IPostHogClient>(),
            hostEnvironment);
    }

    public static TheoryData<HttpStatusCode, string> CodeCases => new()
    {
        { HttpStatusCode.BadRequest, ErrorCode.GeneralError.Value },
        { HttpStatusCode.Unauthorized, ErrorCode.GeneralError.Value },
        { HttpStatusCode.Forbidden, ErrorCode.NotAllowed.Value },
        { HttpStatusCode.NotFound, ErrorCode.NotFound.Value },
        { HttpStatusCode.Conflict, ErrorCode.AlreadyExists.Value },
        { HttpStatusCode.UnprocessableEntity, ErrorCode.ValidationError.Value },
        { HttpStatusCode.TooManyRequests, ErrorCode.TooManyRequests.Value }
    };

    [Theory]
    [MemberData(nameof(CodeCases))]
    public async Task HttpCodeException_WhenThrown_ThenReturnsProblemDetailsWithStatusAndCode(
        HttpStatusCode statusCode,
        string expectedCode)
    {
        var context = CreateHttpContext();
        var exception = new HttpCodeException("Something went wrong", statusCode, new ErrorCode(expectedCode));

        var handled = await HandleAsync(context, exception);

        handled.ShouldBeTrue();
        context.Response.StatusCode.ShouldBe((int)statusCode);
        context.Response.ContentType.ShouldStartWith("application/problem+json");

        var problem = await ReadProblemAsync(context);
        problem.Status.ShouldBe((int)statusCode);
        problem.Title.ShouldNotBeNullOrEmpty();
        problem.Detail.ShouldBe("Something went wrong");
        problem.Type.ShouldBeNull();
        problem.Instance.ShouldBe("/api/v1/test");
        GetCode(problem).ShouldBe(expectedCode);
        problem.Extensions.ShouldContainKey("traceId");
    }

    [Fact]
    public async Task HttpCodeException_WhenAcceptIsNotJson_ThenStillWritesProblemDetailsJson()
    {
        var context = CreateHttpContext();
        context.Request.Headers.Accept = "text/html";

        var exception = new HttpCodeException(
            "Something went wrong",
            HttpStatusCode.BadRequest,
            ErrorCode.ValidationError);

        await HandleAsync(context, exception);

        context.Response.ContentType.ShouldStartWith("application/problem+json");

        var problem = await ReadProblemAsync(context);
        problem.Detail.ShouldBe("Something went wrong");
        GetCode(problem).ShouldBe(ErrorCode.ValidationError.Value);
    }

    [Fact]
    public async Task HttpCodeException_WhenDetailsProvided_ThenUsesCodeAndMergesDetails()
    {
        var venueId = Guid.NewGuid();
        var context = CreateHttpContext();
        var exception = new HttpCodeException(
            "You must be in the venue to check in",
            HttpStatusCode.UnprocessableEntity,
            CustomErrorCode)
        {
            Details = new Dictionary<string, object?>
            {
                ["venueId"] = venueId
            }
        };

        await HandleAsync(context, exception);

        var problem = await ReadProblemAsync(context);
        GetCode(problem).ShouldBe(CustomErrorCode.Value);
        GetVenueId(problem).ShouldBe(venueId);
    }

    [Fact]
    public async Task ArgumentException_WhenThrown_ThenReturnsBadRequestWithValidationError()
    {
        var context = CreateHttpContext();

        await HandleAsync(context, new ArgumentException("Argument is invalid"));

        context.Response.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);

        var problem = await ReadProblemAsync(context);
        problem.Status.ShouldBe(StatusCodes.Status400BadRequest);
        problem.Detail.ShouldBe("Argument is invalid");
        GetCode(problem).ShouldBe(nameof(ErrorCode.ValidationError));
    }

    [Fact]
    public async Task ArgumentNullException_WhenThrown_ThenReturnsBadRequestWithValidationError()
    {
        var context = CreateHttpContext();

        await HandleAsync(context, new ArgumentNullException("value"));

        context.Response.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);

        var problem = await ReadProblemAsync(context);
        GetCode(problem).ShouldBe(nameof(ErrorCode.ValidationError));
    }

    [Fact]
    public async Task FormatException_WhenThrown_ThenReturnsBadRequestWithValidationError()
    {
        var context = CreateHttpContext();

        await HandleAsync(context, new FormatException("Invalid Base64 input"));

        context.Response.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);

        var problem = await ReadProblemAsync(context);
        problem.Status.ShouldBe(StatusCodes.Status400BadRequest);
        GetCode(problem).ShouldBe(nameof(ErrorCode.ValidationError));
    }

    [Fact]
    public async Task UnauthorizedAccessException_WhenThrown_ThenReturnsUnauthorized()
    {
        var context = CreateHttpContext();

        await HandleAsync(context, new UnauthorizedAccessException("No access"));

        context.Response.StatusCode.ShouldBe(StatusCodes.Status401Unauthorized);

        var problem = await ReadProblemAsync(context);
        problem.Status.ShouldBe(StatusCodes.Status401Unauthorized);
        GetCode(problem).ShouldBe(nameof(ErrorCode.GeneralError));
    }

    [Fact]
    public async Task UnknownException_WhenThrown_ThenReturnsGenericServerErrorWithoutLeakingMessage()
    {
        var context = CreateHttpContext();

        await HandleAsync(context, new InvalidOperationException("Sensitive database connection string"));

        context.Response.StatusCode.ShouldBe(StatusCodes.Status500InternalServerError);

        var problem = await ReadProblemAsync(context);
        problem.Status.ShouldBe(StatusCodes.Status500InternalServerError);
        problem.Detail.ShouldBe("An unexpected error occurred.");
        problem.Detail!.ShouldNotContain("Sensitive");
        GetCode(problem).ShouldBe(nameof(ErrorCode.GeneralError));
        problem.Type.ShouldBeNull();
        problem.Instance.ShouldBe("/api/v1/test");
    }

    private async Task<bool> HandleAsync(DefaultHttpContext context, Exception exception)
    {
        return await _handler.TryHandleAsync(context, exception, CancellationToken.None);
    }

    private static DefaultHttpContext CreateHttpContext()
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/v1/test";
        context.Request.Headers.Accept = "application/json";
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static async Task<ProblemDetails> ReadProblemAsync(HttpContext context)
    {
        context.Response.Body.Position = 0;
        var problem = await JsonSerializer.DeserializeAsync<ProblemDetails>(
            context.Response.Body,
            JsonOptions);
        problem.ShouldNotBeNull();
        return problem;
    }

    private static string? GetCode(ProblemDetails problem)
        => GetExtension(problem, "code");

    private static Guid? GetVenueId(ProblemDetails problem)
        => Guid.TryParse(GetExtension(problem, "venueId"), out var id) ? id : null;

    private static string? GetExtension(ProblemDetails problem, string key)
    {
        if (!problem.Extensions.TryGetValue(key, out var value) || value is null)
        {
            return null;
        }

        return value is JsonElement { ValueKind: JsonValueKind.String } element
            ? element.GetString()
            : value.ToString();
    }
}
