using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SharedCore.Api;
using SharedCore.Api.Dto;
using UserProfileModule.API.Dto;
using UserProfileModule.Domain.Aggregates;
using UserProfileModule.Domain.Repositories;

namespace UserProfileModule.API;

public class HobbyEndpoints : IEndpoints
{
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var apiGroup = app.MapGroup("")
            .WithTags("Hobby API")
            .RequireAuthorization();
        apiGroup.MapGet("/api/v1/hobby", GetHobbyList);
    }

    private static async Task<ApiResult<GetHobbyListResponse>> GetHobbyList(
        IHobbyRepository hobbyRepository,
        CancellationToken ct)
    {
        var hobbies = await hobbyRepository.GetAll(ct);
        var dtos = hobbies.Select(h => new HobbyDto(Id: h.Id, Name: h.Name)).ToArray();
        return ApiResult<GetHobbyListResponse>.Build(new GetHobbyListResponse(dtos));
    }
}