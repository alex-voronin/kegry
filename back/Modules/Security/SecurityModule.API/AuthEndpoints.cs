using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using SecurityModule.Application.Services;
using SecurityModule.API.Dto;
using SecurityModule.Application.Configs;
using SecurityModule.Domain.Exceptions;
using SecurityModule.Domain.Repositories;
using SecurityModule.Infrastructure.Service;
using SharedCore.Api;
using SharedCore.Extensions;

namespace SecurityModule.API;

public class AuthEndpoints : IEndpoints
{
    private static readonly string[] HostWhitelist =
    [
        "kegry.com",
        "business.kegry.com",
        "kegry.rs",
        "business.kegry.rs",
        "localhost",
        "localhost:3000",
        "localtest.me",
        "business.localtest.me"
    ];

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
        anonymousGroup.MapPost("/api/v1/auth/resend-confirmation-email", ResendConfirmationEmail);
        anonymousGroup.MapPost("/api/v1/auth/request-password-reset", RequestPasswordReset);
        anonymousGroup.MapPost("/api/v1/auth/confirm-password-reset", ConfirmPasswordReset);

        var authGroup = app.MapGroup("")
            .WithTags("Auth API")
            .RequireAuthorization();
        authGroup.MapPost("/api/v1/auth/logout", Logout);
        authGroup.MapGet("/api/v1/auth/account", GetAccount);
        authGroup.MapDelete("/api/v1/auth/account", DeleteAccount);
    }

    private static async Task<SigninResponse> Signin(
        [FromBody] SignInRequest request,
        IAuthenticationService authenticationService)
    {
        var isExist = await authenticationService.IsUserExistAsync(request.Email);
        if (!isExist)
        {
            return new SigninResponse(IsSignupRequired: true, Auth: null);
        }

        var result = await authenticationService.SigninAsync(email: request.Email, password: request.Password);
        return new SigninResponse(
            IsSignupRequired: false,
            Auth: AuthResultToResponse(result)
        );
    }

    private static async Task<AuthResponse> Signup(
        IAuthenticationService authenticationService,
        [FromBody] SignUpRequest request)
    {
        var result = await authenticationService.SignupAsync(
            email: request.Email,
            password: request.Password,
            userType: request.UserType);
        return AuthResultToResponse(result);
    }

    private static async Task<AuthResponse> SigninWithGoogle(
        [FromBody] GoogleIdTokenRequest request,
        IAuthenticationService authenticationService,
        IGoogleTokenValidator googleTokenValidator,
        IOptions<GoogleAuthConfig> googleAuthConfig)
    {
        var payload = await googleTokenValidator.ValidateAsync(request.IdToken, googleAuthConfig.Value.ClientId);
        var result = await authenticationService.SigninExternalAsync(payload.Email, request.UserType);
        return AuthResultToResponse(result);
    }

    private static async Task<AuthResponse> ConfirmEmail(
        [FromBody] ConfirmEmailRequest request,
        IAuthenticationService authenticationService,
        CancellationToken ct)
    {
        var result = await authenticationService.ConfirmEmailAsync(userId: request.UserId, token: request.Token);
        return AuthResultToResponse(result);
    }

    private static async Task<NoContent> ResendConfirmationEmail(
        [FromBody] ResendConfirmationEmailRequest request,
        IAuthenticationService service)
    {
        await service.ResendConfirmationEmailAsync(request.UserId);
        return TypedResults.NoContent();
    }

    private static async Task<AuthResponse> RenewTokens(
        [FromBody] RenewTokensRequest request,
        IAuthenticationService service)
    {
        var result = await service.RenewTokensAsync(request.RefreshToken);
        return AuthResultToResponse(result);
    }

    private static async Task<NoContent> Logout(
        [FromBody] LogoutRequest request,
        IAuthenticationService service)
    {
        await service.LogoutAsync(request.DeviceId);
        return TypedResults.NoContent();
    }

    private static async Task<NoContent> RequestPasswordReset(
        [FromBody] RequestPasswordResetRequest request,
        HttpRequest httpRequest,
        IAuthenticationService service)
    {
        if (!HostWhitelist.Contains(httpRequest.Host.ToString()))
        {
            throw SecurityExceptions.UntrustedHostException();
        }

        var baseUrl = httpRequest.Scheme + "://" + httpRequest.Host;
        await service.RequestPasswordResetAsync(request.Email, baseUrl);
        return TypedResults.NoContent();
    }

    private static async Task<NoContent> ConfirmPasswordReset(
        [FromBody] ConfirmPasswordResetRequest request,
        IAuthenticationService service)
    {
        var decodedEmail = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(request.EncodedEmail));
        var decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(request.EncodedToken));
        await service.ConfirmPasswordResetAsync(decodedEmail, decodedToken, request.NewPassword);
        return TypedResults.NoContent();
    }

    private static async Task<GetAccountResponse> GetAccount(
        ClaimsPrincipal user,
        IUserRepository userRepository,
        CancellationToken ct)
    {
        var userId = user.GetId();
        var userEntity = await userRepository.GetByIdAsync(userId, ct: ct);
        if (userEntity == null)
        {
            throw SecurityExceptions.UserNotFound(userId);
        }

        return new GetAccountResponse(
            Id: userEntity.Id,
            Type: userEntity.Type,
            Email: userEntity.Email!);
    }

    private static async Task<NoContent> DeleteAccount(
        ClaimsPrincipal user,
        IAuthenticationService service)
    {
        var userId = user.GetId();
        await service.DeleteAccountAsync(userId.ToString());
        return TypedResults.NoContent();
    }

    #region DTO converters

    private static AuthResponse AuthResultToResponse(AuthResult result)
    {
        return new AuthResponse(
            UserId: result.User.Id.ToString(),
            UserType: result.User.Type,
            RequiredAction: result.User.RequiredAction,
            AccessToken: result.AccessToken,
            RefreshToken: result.RefreshToken
        );
    }

    #endregion
}