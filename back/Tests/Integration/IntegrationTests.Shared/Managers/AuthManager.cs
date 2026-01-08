using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using SecurityModule.API.Dto;
using SecurityModule.Domain.Aggregates.User;
using SecurityModule.Infrastructure.Persistence;
using SharedCore.Api.Dto;
using Shouldly;

namespace IntegrationTests.Shared.Managers;

public class AuthManager
{
    private readonly IntegrationApiClient _apiClient;
    private readonly IServiceProvider _serviceProvider;

    public AuthManager(IntegrationApiClient apiClient, IServiceProvider serviceProvider)
    {
        _apiClient = apiClient;
        _serviceProvider = serviceProvider;
    }

    public async Task<string> CreateAndLoginUserAsync(string email, string password)
    {
        // 1. Sign up
        var request = new SignInRequest(email, password);

        var signinResponse = await _apiClient.PostAsync<SignInRequest, ApiResult<AuthResponse>>(
            "/api/v1/auth/signin", request);

        signinResponse.Success.ShouldBeTrue();
        var userId = Guid.Parse(signinResponse.Result.UserId);

        // 2. Manually verify user in DB
        using (var scope = _serviceProvider.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<SecurityContext>();
            var user = await context.Users.FindAsync(userId);
            user.ShouldNotBeNull();
            user.RequiredAction = RequiredAction.None;
            user.EmailConfirmed = true;
            await context.SaveChangesAsync();
        }
        
        // 3. Sign in again to get access token
        var loginResponse = await _apiClient.PostAsync<SignInRequest, ApiResult<AuthResponse>>(
            "/api/v1/auth/signin", request);

        loginResponse.Success.ShouldBeTrue(
            $"Login failed with errors: {string.Join(", ", loginResponse.Errors.Select(e => e.Error))}");
        loginResponse.Result.AccessToken.ShouldNotBeNullOrEmpty();
        
        return loginResponse.Result.AccessToken;
    }
}