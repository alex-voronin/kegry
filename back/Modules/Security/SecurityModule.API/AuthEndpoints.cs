using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using Google.Apis.Auth;
using Microsoft.Extensions.Options;
using SecurityModule.Application.Services;
using SecurityModule.API.Dto;
using SecurityModule.Application.Configs;
using SharedCore.Api;
using SharedCore.Api.Dto;
using SharedCore.Extensions;

namespace SecurityModule.API;

public class AuthEndpoints : IEndpoints
{
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var anonymousGroup = app.MapGroup("")
            .WithTags("Auth API")
            .AllowAnonymous();
        anonymousGroup.MapPost("/api/v1/auth/signin", Signin);
        anonymousGroup.MapPost("/api/v1/auth/signup", Signup);
        anonymousGroup.MapPost("/api/v1/auth/google", SigninWithGoogle);
        anonymousGroup.MapPost("/api/v1/auth/renew-tokens", RenewTokens);
        anonymousGroup.MapPost("/api/v1/auth/confirm-email", ConfirmEmail);
        anonymousGroup.MapPost("/api/v1/auth/request-password-reset", RequestPasswordReset);
        anonymousGroup.MapPost("/api/v1/auth/confirm-password-reset", ConfirmPasswordReset);

        var authGroup = app.MapGroup("")
            .WithTags("Auth API")
            .RequireAuthorization();
        authGroup.MapPost("/api/v1/auth/logout", Logout);
        authGroup.MapDelete("/api/v1/auth/account", DeleteAccount);
    }

    private static async Task<ApiResult<SigninResponse>> Signin(
        [FromBody] SignInRequest request,
        IAuthenticationService authenticationService)
    {
        var isExist = await authenticationService.IsUserExistAsync(request.Email);
        if (!isExist)
        {
            return ApiResult<SigninResponse>.Build(
                new SigninResponse(IsSignupRequired: true, Auth: null)
            );
        }

        var result = await authenticationService.SigninAsync(email: request.Email, password: request.Password);
        return ApiResult<SigninResponse>.Build(
            new SigninResponse(
                IsSignupRequired: false,
                Auth: AuthResultToResponse(result)
            )
        );
    }

    private static async Task<ApiResult<AuthResponse>> Signup(
        IAuthenticationService authenticationService,
        [FromBody] SignInRequest request)
    {
        var result = await authenticationService.SignupAsync(email: request.Email, password: request.Password);
        return ApiResult<AuthResponse>.Build(
            AuthResultToResponse(result)
        );
    }

    private static async Task<ApiResult<AuthResponse>> SigninWithGoogle(
        [FromBody] GoogleIdTokenRequest request,
        IAuthenticationService authenticationService,
        IOptions<GoogleAuthConfig> googleAuthConfig)
    {
        var settings = new GoogleJsonWebSignature.ValidationSettings
        {
            Audience = [googleAuthConfig.Value.ClientId]
        };

        var payload = await GoogleJsonWebSignature.ValidateAsync(request.IdToken, settings);

        var result = await authenticationService.SigninExternalAsync(payload.Email);
        return ApiResult<AuthResponse>.Build(
            AuthResultToResponse(result)
        );
    }

    private static async Task<ApiResult<AuthResponse>> ConfirmEmail(
        [FromBody] ConfirmEmailRequest request,
        IAuthenticationService authenticationService,
        CancellationToken ct)
    {
        var result = await authenticationService.ConfirmEmailAsync(userId: request.UserId, token: request.Token);
        return ApiResult<AuthResponse>.Build(
            AuthResultToResponse(result)
        );
    }

    private static async Task<ApiResult<AuthResponse>> RenewTokens(
        [FromBody] RenewTokensRequest request,
        IAuthenticationService service)
    {
        var result = await service.RenewTokensAsync(request.RefreshToken);
        return ApiResult<AuthResponse>.Build(
            AuthResultToResponse(result)
        );
    }

    private static async Task<ApiResult.EmptyResult> Logout(IAuthenticationService service)
    {
        await service.LogoutAsync();
        return ApiResult.Empty;
    }

    private static async Task<ApiResult.EmptyResult> RequestPasswordReset(
        [FromBody] RequestPasswordResetRequest request,
        IAuthenticationService service)
    {
        await service.RequestPasswordResetAsync(request.Email);
        return ApiResult.Empty;
    }

    private static async Task<ApiResult.EmptyResult> ConfirmPasswordReset(
        [FromBody] ConfirmPasswordResetRequest request,
        IAuthenticationService service)
    {
        var decodedEmail = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(request.EncodedEmail));
        var decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(request.EncodedToken));
        await service.ConfirmPasswordResetAsync(decodedEmail, decodedToken, request.NewPassword);
        return ApiResult.Empty;
    }

    private static async Task<ApiResult.EmptyResult> DeleteAccount(
        ClaimsPrincipal user,
        IAuthenticationService service)
    {
        var userId = user.GetId();
        await service.DeleteAccountAsync(userId.ToString());
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