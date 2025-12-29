using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SharedCore.Api;
using SharedCore.Api.Dto;
using UserProfileModule.API.Dto;
using UserProfileModule.Domain.Aggregates;

namespace UserProfileModule.API;

public class HobbyEndpoints : IEndpoints
{
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var authGroup = app.MapGroup("")
            .RequireAuthorization();
        authGroup.MapGet("/api/v1/hobby", GetHobbyList)
            .RequireAuthorization();
    }

    private static async Task<ApiResult<GetHobbyListResponse>> GetHobbyList(
        IHobbyRepository hobbyRepository,
        CancellationToken ct)
    {
        var hobbies = await hobbyRepository.GetAll(ct);
        var dtos = hobbies.Select(h => new HobbyDto()
            {
                Id = h.Id,
                Name = h.Name,
            })
            .ToArray();
        return ApiResult<GetHobbyListResponse>.Build(
            new GetHobbyListResponse
            {
                Hobbies = dtos
            }
        );
    }
}