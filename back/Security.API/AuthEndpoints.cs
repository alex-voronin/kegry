using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Security.API.Dto;
using Security.Application.Services;
using SharedCore.Api;
using SharedCore.Api.Dto;

namespace Security.API;

public class AuthEndpoints : IEndpoints
{
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPost("/signin", SignIn).AllowAnonymous();
    }

    private static async Task<ApiResult<SignInResponse>> SignIn(
        IAuthenticationService service,
        [FromBody] SignInRequest request)
    {
        var result = await service.SigninAsync(email: request.Email, password: request.Password);
        return ApiResult<SignInResponse>.Build(
            new SignInResponse
            {
                UserId = result.User.Id.ToString(),
                RequiredAction =  result.User.RequiredAction,
                AccessToken = result.AccessToken,
                RefreshToken = result.RefreshToken,
                
            }
        );
    }
}