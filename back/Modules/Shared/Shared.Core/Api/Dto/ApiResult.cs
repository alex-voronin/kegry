using System.Text.Json.Serialization;

namespace SharedCore.Api.Dto;

public class ApiResult<TResult>
{
    public TResult Result { get; init; }

    [JsonConstructor]
    protected ApiResult(TResult result)
    {
        Result = result;
    }

    public static ApiResult<TResult> Build(TResult result) => new(result);
}

public static class ApiResult
{
    public sealed class EmptyResult : ApiResult<EmptyResponse>
    {
        public EmptyResult() : base(EmptyResponse.Empty)
        {
        }
    }

    public static EmptyResult Empty { get; } = new EmptyResult();
}
