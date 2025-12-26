using System.Text.Json.Serialization;
using SharedCore.Exceptions;

namespace SharedCore.Api.Dto;

public class ApiError
{
    public required string Error { get; init; }
    public string Code { get; init; } = nameof(ErrorCode.GeneralError);
}

public class ApiResult<TResult>
{
    public bool Success { get; init; }
    public ApiError[] Errors { get; init; }
    public TResult Result { get; init; }

    protected ApiResult(TResult result, params string[] errors)
    {
        var apiErrors = errors
            .Select(x =>
                new ApiError()
                {
                    Error = x,
                })
            .ToArray();
        Result = result;
        Errors = apiErrors;
        Success = errors.Length == 0;
    }
    
    [JsonConstructor]
    protected ApiResult(TResult result, params ApiError[] errors)
    {
        Result = result;
        Errors = errors;
        Success = errors.Length == 0;
    }

    public static ApiResult<TResult> Build(TResult result)
    {
        return new ApiResult<TResult>(result, errors: Array.Empty<ApiError>());
    }
}

public static class ApiResult
{
    public sealed class EmptyResult : ApiResult<EmptyResponse>
    {
        public EmptyResult() : base(EmptyResponse.Empty, Array.Empty<ApiError>()) { }
        public EmptyResult(params string[] errors) : base(EmptyResponse.Empty, errors) { }
    
        public EmptyResult(params ApiError[] errors) : base(EmptyResponse.Empty, errors) { }
    }
    
    public static EmptyResult Empty { get; } = new EmptyResult();
}