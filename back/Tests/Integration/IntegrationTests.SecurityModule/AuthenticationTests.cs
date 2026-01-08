using System.IdentityModel.Tokens.Jwt;
using System.Text;
using IntegrationTests.Shared;
using Microsoft.IdentityModel.Tokens;
using NotificationsModule.Infrastructure.Transport.Emails;
using NSubstitute;
using SecurityModule.API.Dto;
using SecurityModule.Domain.Aggregates;
using SharedCore.Api.Dto;
using SharedCore.Exceptions;
using Shouldly;

namespace IntegrationTests.SecurityModule;

[Collection("IntegrationTests")]
public class AuthenticationTests : IAsyncLifetime
{
    public Task InitializeAsync() => Task.CompletedTask;
    public async Task DisposeAsync() => await _factory.ResetDatabaseAsync();

    private readonly IntegrationWebApplicationFactory _factory;

    public AuthenticationTests(IntegrationWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GivenNoUsers_WhenUserSignsUp_ThenUserIsRedirectedToEmailVerificationAndEmailIsSent()
    {
        var email = "asd@gmail.com";
        var request = new SignUpRequest(Email: email, Password: "123456789Qwe");

        var signinResponse = await _factory.ApiClient.PostAsync<SignUpRequest, ApiResult<AuthResponse>>(
            "/api/v1/auth/signup", request);

        signinResponse.Success.ShouldBeTrue();
        signinResponse.Result.RequiredAction.ShouldBe(RequiredAction.EmailVerification);
        signinResponse.Result.AccessToken.ShouldBeNull();
        signinResponse.Result.RefreshToken.ShouldBeNull();
        await _factory.NotificationManager.EmailSender
            .Received(1)
            .SendEmailAsync(Arg.Is<Email>(e =>
                    e.ToEmail == email &&
                    e.Subject.Contains("Confirm") &&
                    e.Body.Contains("code")),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GivenNoUsers_WhenUserSignsUpAndPassesInvalidEmail_ThenEmailErrorIsReturned()
    {
        var email = "asd@asldk.com";
        var request = new SignUpRequest(Email: email, Password: "123456789Qwe");

        var signinResponse = await _factory.ApiClient.PostAsync<SignUpRequest, ApiResult<AuthResponse>>(
            "/api/v1/auth/signup", request);

        signinResponse.Success.ShouldBeFalse();
        signinResponse.Errors.First().Code.ShouldBe(nameof(ErrorCode.DoesNotExist));
    }
    
    [Fact]
    public async Task GivenNoUsers_WhenUserSignsIn_ThenUserIsRedirectedToSignup()
    {
        var request = new SignInRequest(Email: "asd@gmail.com", Password: "123456789Qwe");

        var signinResponse = await _factory.ApiClient.PostAsync<SignInRequest, ApiResult<SigninResponse>>(
            "/api/v1/auth/signin", request);

        signinResponse.Success.ShouldBeTrue();
        signinResponse.Result.Auth.ShouldBeNull();
        signinResponse.Result.IsSignupRequired.ShouldBeTrue();
    }

    [Fact]
    public async Task GivenUser_WhenUserSignsIn_ThenValidAuthTokenIsReturned()
    {
        var email = "asd@gmail.com";
        var password = "123456789Qwe";
        var authResponse = await _factory.AuthManager.CreateAndLoginUserAsync(email, password);

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
}