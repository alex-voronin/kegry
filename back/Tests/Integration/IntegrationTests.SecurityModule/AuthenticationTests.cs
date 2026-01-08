using IntegrationTests.Shared;
using SecurityModule.API.Dto;
using SecurityModule.Domain.Aggregates.User;
using SharedCore.Api.Dto;
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
    public async Task AuthenticationTests_SignupShouldSuccess_WhenUserNotExists()
    {
        var request = new SignInRequest(Email: "asd@gmail.com", Password: "123456789Qwe");
        
        var signinResponse = await _factory.ApiClient.PostAsync<SignInRequest, ApiResult<AuthResponse>>(
            "/api/v1/auth/signin", request);

        signinResponse.Success.ShouldBeTrue();
        signinResponse.Result.RequiredAction.ShouldBe(RequiredAction.EmailVerification);
        signinResponse.Result.AccessToken.ShouldBeNull();
        signinResponse.Result.RefreshToken.ShouldBeNull();
    }
}