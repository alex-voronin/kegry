using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using SecurityModule.Application.Services;
using SecurityModule.API.Dto;
using SharedCore.Api;
using SharedCore.Api.Dto;

namespace SecurityModule.API;

public class AuthEndpoints : IEndpoints
{
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var anonymousGroup = app.MapGroup("")
            .WithTags("Auth API")
            .AllowAnonymous();
        anonymousGroup.MapPost("/api/v1/auth/signin", Signin);
        anonymousGroup.MapPost("/api/v1/auth/renew-tokens", RenewTokens);
        anonymousGroup.MapPost("/api/v1/auth/confirm-email", ConfirmEmail);
        
        app.MapPost("/api/v1/auth/logout", Logout)
            .RequireAuthorization()
            .WithTags("Auth API");
    }

    private static async Task<ApiResult<AuthResponse>> Signin(
        IAuthenticationService authenticationService,
        [FromBody] SignInRequest request)
    {
        var result = await authenticationService.SigninAsync(email: request.Email, password: request.Password);
        return ApiResult<AuthResponse>.Build(
            AuthResultToResponse(result)
        );
    }
    
    private static async Task<ApiResult<AuthResponse>> ConfirmEmail(
        IAuthenticationService authenticationService,
        [FromBody] ConfirmEmailRequest request,
        CancellationToken ct)
    {
        var result = await authenticationService.ConfirmEmailAsync(userId: request.UserId, token: request.Token);
        return ApiResult<AuthResponse>.Build(
            AuthResultToResponse(result)
        );
    }
    
    private static async Task<ApiResult<AuthResponse>> RenewTokens(
        IAuthenticationService service,
        [FromBody] RenewTokensRequest request)
    {
        var result = await service.RenewTokensAsync(request.RefreshToken);
        return ApiResult<AuthResponse>.Build(
            AuthResultToResponse(result)
        );
    }
    
    private static async Task<ApiResult.EmptyResult> Logout(
        IAuthenticationService service,
        [FromBody] SignInRequest request)
    {
        await service.LogoutAsync();
        return ApiResult.Empty;
    }

    #region DTO converters

    private static AuthResponse AuthResultToResponse(AuthResult result)
    {
        return new AuthResponse(
            UserId: result.User.Id.ToString(),
            RequiredAction: result.User.RequiredAction,
            AccessToken: result.AccessToken,
            RefreshToken: result.RefreshToken
        );
    }

    #endregion
}