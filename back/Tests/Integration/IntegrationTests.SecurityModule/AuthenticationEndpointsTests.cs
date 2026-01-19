using System.IdentityModel.Tokens.Jwt;
using System.Text;
using IntegrationTests.Shared;
using Microsoft.IdentityModel.Tokens;
using System.Text.RegularExpressions;
using NotificationsModule.Infrastructure.Transport.Emails;
using NSubstitute;
using NSubstitute.ClearExtensions;
using SecurityModule.API.Dto;
using SecurityModule.Domain.Aggregates;
using SharedCore.Api.Dto;
using SharedCore.Exceptions;
using Shouldly;

namespace IntegrationTests.SecurityModule;

[Collection("IntegrationTests")]
public class AuthenticationEndpointsTests : IntegrationTestBase, IAsyncLifetime
{
    public AuthenticationEndpointsTests(IntegrationWebApplicationFactory factory) : base(factory)
    {
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public async Task DisposeAsync() => await ContainerManager.ResetDatabaseAsync();

    [Fact]
    public async Task GivenNoUsers_WhenUserSignsUp_ThenUserIsRedirectedToEmailVerificationAndEmailIsSent()
    {
        string capturedCode = null!;
        await Factory.NotificationManager.EmailSender
            .SendEmailAsync(Arg.Do<Email>(e =>
            {
                var match = Regex.Match(e.Body, @"is (\d+)");
                if (match.Success)
                {
                    capturedCode = match.Groups[1].Value;
                }
            }), Arg.Any<CancellationToken>());

        // 1. Signup
        var email = "asd@gmail.com";
        var request = new SignUpRequest(Email: email, Password: "123456789Qwe");

        var signinResponse = await Factory.ApiClient.PostAsync<SignUpRequest, ApiResult<AuthResponse>>(
            ApiRoutes.Auth.Signup, request);

        // 2. Check signup response
        signinResponse.Success.ShouldBeTrue();
        signinResponse.Result.RequiredAction.ShouldBe(RequiredAction.EmailVerification);
        signinResponse.Result.AccessToken.ShouldBeNull();
        signinResponse.Result.RefreshToken.ShouldBeNull();

        capturedCode.ShouldNotBeNullOrEmpty();

        // 3. Confirm Email
        var confirmRequest = new ConfirmEmailRequest(signinResponse.Result.UserId, capturedCode);
        var confirmResponse = await Factory.ApiClient.PostAsync<ConfirmEmailRequest, ApiResult<AuthResponse>>(
            ApiRoutes.Auth.ConfirmEmail, confirmRequest);

        // 4. Check confirmation response
        confirmResponse.Success.ShouldBeTrue();
        confirmResponse.Result.AccessToken.ShouldNotBeNullOrEmpty();
        confirmResponse.Result.RequiredAction.ShouldBe(RequiredAction.OnboardingCompletion);
    }

    [Fact]
    public async Task GivenNoUsers_WhenUserSignsUpAndPassesInvalidEmail_ThenEmailErrorIsReturned()
    {
        var email = "asd@asldk.com";
        var request = new SignUpRequest(Email: email, Password: "123456789Qwe");

        var signinResponse = await Factory.ApiClient.PostAsync<SignUpRequest, ApiResult<AuthResponse>>(
            ApiRoutes.Auth.Signup, request);

        signinResponse.Success.ShouldBeFalse();
        signinResponse.Errors.First().Code.ShouldBe(nameof(ErrorCode.DoesNotExist));
    }

    [Fact]
    public async Task GivenNoUsers_WhenUserSignsIn_ThenUserIsRedirectedToSignup()
    {
        var request = new SignInRequest(Email: "asd@gmail.com", Password: "123456789Qwe");

        var signinResponse = await Factory.ApiClient.PostAsync<SignInRequest, ApiResult<SigninResponse>>(
            ApiRoutes.Auth.Signin, request);

        signinResponse.Success.ShouldBeTrue();
        signinResponse.Result.Auth.ShouldBeNull();
        signinResponse.Result.IsSignupRequired.ShouldBeTrue();
    }

    [Fact]
    public async Task GivenUser_WhenUserSignsIn_ThenValidAuthTokenIsReturned()
    {
        var email = "asd@gmail.com";
        var password = "123456789Qwe";
        var authResponse = await Factory.AuthManager.CreateAndLoginUserAsync(email, password);

        var handler = new JwtSecurityTokenHandler()
        {
            // disable automatic claims mapping to legacy names
            MapInboundClaims = false,
        };

        var parameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = TestConfig.AuthToken.Issuer,
            ValidateAudience = true,
            ValidAudience = TestConfig.AuthToken.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(TestConfig.AuthToken.Secret)
            ),
            ValidateLifetime = true,
        };

        var principal = handler.ValidateToken(authResponse.AccessToken, parameters, out var validated);
        validated.ShouldBeAssignableTo(typeof(JwtSecurityToken));
        principal.Claims.ShouldContain(claim => claim.Type == JwtRegisteredClaimNames.Sub &&
                                                claim.Value == authResponse.UserId);
        principal.Claims.ShouldContain(claim => claim.Type == JwtRegisteredClaimNames.Email && claim.Value == email);
    }

    [Fact]
    public async Task GivenUserWithResetToken_WhenPasswordResetConfirmed_ThenCanSignInWithNewPassword()
    {
        string capturedToken = null!;
        await Factory.NotificationManager.EmailSender
            .SendEmailAsync(Arg.Do<Email>(e =>
            {
                var match = Regex.Match(e.Body, @"<a href=""https:\/\/hoppyfy\.me\/password-reset\?email=.+&token=(.+)"">");
                capturedToken = match.Groups[1].Value;
            }), Arg.Any<CancellationToken>());

        // 1. Authenticate
        var email = "user@example.com";
        var oldPassword = "OldPassword123!";
        var newPassword = "NewPassword123!";
        await Factory.AuthManager.CreateAndLoginUserAsync(email, oldPassword);

        // 2. Reset password
        var requestReset = new RequestPasswordResetRequest(email);
        await Factory.ApiClient.PostAsync<RequestPasswordResetRequest, ApiResult.EmptyResult>(
            ApiRoutes.Auth.RequestPasswordReset, requestReset);

        capturedToken.ShouldNotBeNullOrEmpty();

        // 3. Confirm password reset 
        var confirmRequest = new ConfirmPasswordResetRequest(
            Base64UrlEncoder.Encode(email), capturedToken, newPassword);
        var confirmResponse = await Factory.ApiClient.PostAsync<ConfirmPasswordResetRequest, ApiResult.EmptyResult>(
            ApiRoutes.Auth.ConfirmPasswordReset, confirmRequest);

        confirmResponse.Success.ShouldBeTrue();

        // 4. Ensure that we can sign in with the new password
        var signinRequest = new SignInRequest(email, newPassword);
        var signinResponse = await Factory.ApiClient.PostAsync<SignInRequest, ApiResult<SigninResponse>>(
            ApiRoutes.Auth.Signin, signinRequest);
        signinResponse.Success.ShouldBeTrue();
    }

    [Fact]
    public async Task GivenEmailForNonExistingUser_WhenRequestsPasswordReset_ThenEmailIsNotSent()
    {
        Factory.NotificationManager.EmailSender.ClearSubstitute();

        // 1. Reset password
        var requestReset = new RequestPasswordResetRequest("test@test.com");
        var resetResponse = await Factory.ApiClient.PostAsync<RequestPasswordResetRequest, ApiResult.EmptyResult>(
            ApiRoutes.Auth.RequestPasswordReset, requestReset);
        resetResponse.Success.ShouldBeTrue();

        // 2. Ensure that we haven't sent any emails
        await Task.Delay(1000);
        await Factory.NotificationManager.EmailSender.DidNotReceive()
            .SendEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GivenUser_WhenPasswordResetConfirmedWithInvalidToken_ThenReturnsError()
    {
        // 1. Authenticate
        var email = "user@example.com";
        var auth = await Factory.AuthManager.CreateAndLoginUserAsync(email, "Password123!");
        var userId = auth.UserId;

        // 2. Confirm password reset
        var confirmRequest = new ConfirmPasswordResetRequest(userId, "invalid-token", "NewPassword123!");
        var confirmResponse = await Factory.ApiClient.PostAsync<ConfirmPasswordResetRequest, ApiResult.EmptyResult>(
            ApiRoutes.Auth.ConfirmPasswordReset, confirmRequest);

        // 3. Check response
        confirmResponse.Success.ShouldBeFalse();
    }

    [Fact]
    public async Task GivenValidAccessToken_WhenRenewTokensWithValidTokenRefresh_ThenReturnsNewTokens()
    {
        // 1. Authenticate
        var email = "user@example.com";
        var auth = await Factory.AuthManager.CreateAndLoginUserAsync(email, "Password123!");

        auth.RefreshToken.ShouldNotBeNullOrEmpty();

        // 2. Renew token
        var request = new RenewTokensRequest(auth.RefreshToken!);
        var response = await Factory.ApiClient.PostAsync<RenewTokensRequest, ApiResult<AuthResponse>>(
            ApiRoutes.Auth.RenewTokens, request);

        response.Success.ShouldBeTrue();
        response.Result.AccessToken.ShouldNotBeNullOrEmpty();
        response.Result.RefreshToken.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task GivenExpiredRefreshToken_WhenRenewTokensWithValidTokenRefresh_ThenReturnsError()
    {
        // 1. Authenticate
        Factory.FakeTimeProvider.Reset(
            DateTime.UtcNow - TimeSpan.FromDays(TestConfig.AuthToken.RefreshTokenDurationInDays + 1)
        );
        var email = "user@example.com";
        var auth = await Factory.AuthManager.CreateAndLoginUserAsync(email, "Password123!");
        auth.RefreshToken.ShouldNotBeNullOrEmpty();

        // 2. Renew token
        Factory.FakeTimeProvider.Reset(DateTime.UtcNow);
        var request = new RenewTokensRequest(auth.RefreshToken!);
        var response = await Factory.ApiClient.PostAsync<RenewTokensRequest, ApiResult<AuthResponse>>(
            ApiRoutes.Auth.RenewTokens, request);

        // 3. Check that 
        response.Success.ShouldBeFalse();
        response.Errors[0].Error.ShouldBe("Invalid or revoked access token");
    }

    [Fact]
    public async Task GivenUser_WhenRenewTokensWithInvalidRefreshToken_ThenReturnsError()
    {
        var request = new RenewTokensRequest("invalid-refresh-token");
        var response = await Factory.ApiClient.PostAsync<RenewTokensRequest, ApiResult<AuthResponse>>(
            ApiRoutes.Auth.RenewTokens, request);

        response.Success.ShouldBeFalse();
    }

    [Fact]
    public async Task GivenUser_WhenAccountDeleted_ThenCannotSignIn()
    {
        // 1. Authenticate
        var email = "user@example.com";
        var password = "Password123!";
        var auth = await Factory.AuthManager.CreateAndLoginUserAsync(email, password);
        Factory.ApiClient.SetAuth(auth);

        // 2. Delete account
        var deleteResponse = await Factory.ApiClient.DeleteAsync<ApiResult.EmptyResult>(ApiRoutes.Auth.Account);
        deleteResponse.Success.ShouldBeTrue();

        // 3. Sign in again
        var signinRequest = new SignInRequest(email, password);
        var signinResponse = await Factory.ApiClient.PostAsync<SignInRequest, ApiResult<SigninResponse>>(
            ApiRoutes.Auth.Signin, signinRequest);

        // 4. Ensure that signup is required 
        signinResponse.Success.ShouldBeTrue();
        signinResponse.Result.IsSignupRequired.ShouldBeTrue();
    }

    [Fact]
    public async Task GivenUser_WhenLoggedOut_ThenInvalidatesRefreshToken()
    {
        // 1. Authenticate
        var auth = await Factory.AuthManager.CreateAndLoginUserAsync("user@example.com", "Password123!");
        Factory.ApiClient.SetAuth(auth);

        // 2. Log out
        var logoutResponse = await Factory.ApiClient
            .PostAsync<object?, ApiResult.EmptyResult>(ApiRoutes.Auth.Logout, null);
        logoutResponse.Success.ShouldBeTrue();

        // 3. Renew token
        var request = new RenewTokensRequest(auth.RefreshToken!);
        var response = await Factory.ApiClient.PostAsync<RenewTokensRequest, ApiResult<AuthResponse>>(
            ApiRoutes.Auth.RenewTokens, request);

        response.Success.ShouldBeFalse();
        response.Errors[0].Error.ShouldBe("Invalid or revoked access token");
    }

    [Fact]
    public async Task GivenUser_WhenLogsInWithInvalidPassword_ThenErrorReturns()
    {
        // 1. Authenticate
        var email = "user@example.com";
        var auth = await Factory.AuthManager.CreateAndLoginUserAsync(email, "Password123!");
        Factory.ApiClient.SetAuth(auth);

        // 2. Log out
        var logoutResponse = await Factory.ApiClient
            .PostAsync<object?, ApiResult.EmptyResult>(ApiRoutes.Auth.Logout, null);
        logoutResponse.Success.ShouldBeTrue();

        // 3. Sign in again
        var request = new SignInRequest(Email: email, Password: "123456789Qwe");
        var signinResponse = await Factory.ApiClient.PostAsync<SignInRequest, ApiResult<SigninResponse>>(
            ApiRoutes.Auth.Signin, request);

        signinResponse.Success.ShouldBeFalse();
    }
}