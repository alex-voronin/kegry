using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using SharedCore.Api;
using SharedCore.Api.Dto;
using SharedCore.Extensions;
using UserProfiles.API.Dto;
using UserProfiles.Domain.Aggregates;
using UserProfiles.Domain.Exceptions;

namespace UserProfiles.API;

public class HobbyEndpoints : IEndpoints
{
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/hobby", GetHobbyList);
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