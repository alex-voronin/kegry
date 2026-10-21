using System.Security.Claims;
using LocationModule.API.Dto;
using LocationModule.Domain.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using SharedCore.Api;
using SharedCore.Extensions;

namespace LocationModule.API;

public class LocationEndpoints : IEndpoints
{
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/location", UpdateLocation)
            .WithTags("Location API")
            .RequireAuthorization();
    }

    private static async Task<NoContent> UpdateLocation(
        [FromBody] UpdateLocationRequest request,
        ClaimsPrincipal user,
        ILocationService locationService)
    {
        await locationService.UpdateLocation(user.GetId(), request.Latitude, request.Longitude);
        return TypedResults.NoContent();
    }
}