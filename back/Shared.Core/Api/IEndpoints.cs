using Microsoft.AspNetCore.Routing;

namespace SharedCore.Api;

public interface IEndpoints
{
    static abstract void MapEndpoints(IEndpointRouteBuilder app);
}