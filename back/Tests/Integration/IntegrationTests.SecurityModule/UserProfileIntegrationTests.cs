using IntegrationTests.Shared;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SecurityModule.Domain.Aggregates;
using Shouldly;
using Wolverine.Tracking;


namespace IntegrationTests.SecurityModule;

[Collection("IntegrationTests")]
public class UserProfileIntegrationTests : IntegrationTestBase, IAsyncLifetime
{
    public UserProfileIntegrationTests(IntegrationWebApplicationFactory factory) : base(factory)
    {
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public async Task DisposeAsync() => await ContainerManager.ResetDatabaseAsync();

    [Fact]
    public async Task GivenUserWithRequiredAction_WhenUserProfileChangedEventIsNewProfile_ThenRequiredActionIsSetToNone()
    {
        // 1. Create User
        var email = "test@example.com";
        var auth = await Factory.AuthManager.CreateAndLoginUserAsync(email, "Password123!");
        Factory.ApiClient.SetAuth(auth);
        await Factory.Host.ExecuteAndWaitAsync(async () =>
        {
            await Factory.AuthManager.CreateDefaultProfileAndSubmitLocation();
        });

        // 2. Ensure that RequiredAction is none
        using var scope = Factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var user = await userManager.FindByIdAsync(auth.UserId);
        user!.RequiredAction.ShouldBe(RequiredAction.None);
    }
}
