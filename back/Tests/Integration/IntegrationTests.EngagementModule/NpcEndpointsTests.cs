using EngagementModule.API.Dto;
using EngagementModule.Domain.Repositories;
using EngagementModule.Domain.Services;
using IntegrationTests.Shared;
using Microsoft.Extensions.DependencyInjection;
using NotificationModule.Domain.ValueObjects;
using Shouldly;

namespace IntegrationTests.EngagementModule;

[Collection("IntegrationTests")]
public class NpcEndpointsTests : IntegrationTestBase, IAsyncLifetime
{
    public NpcEndpointsTests(IntegrationWebApplicationFactory factory) : base(factory)
    {
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public async Task DisposeAsync() => await ContainerManager.ResetDatabaseAsync();

    [Fact]
    public async Task GivenUser_WhenGetNpc_ThenReturnsNpcAndSetsSeenNpcAt()
    {
        // 1. Create user
        var auth = await Factory.AuthManager.CreateAndLoginUserAsync("npc_user@example.com", "Password123!");
        Factory.ApiClient.SetAuth(auth);
        await Factory.AuthManager.CreateDefaultProfileAndSubmitLocation();

        // 2. Get Npc
        var response = await Factory.ApiClient.GetAsync<GetNpcResponse>(ApiRoutes.Npc.Root);
        response.ShouldNotBeNull();
        response.Npc.ShouldNotBeNull();
        response.Npc.Id.ShouldNotBeNullOrEmpty();
        response.Npc.Name.ShouldNotBeNullOrEmpty();

        // 3. Verify SeenNpcAt is updated
        using var scope = Factory.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var user = await repo.GetByIdAsync(Guid.Parse(auth.UserId));
        user.ShouldNotBeNull();
        user.SeenNpcAt.ShouldNotBe(default);
    }

    [Fact]
    public async Task GivenUser_WhenGetNpcBeforeTimeoutAndAfterTimeout_ThenReturnsNpcOrNull()
    {
        // 1. Create user
        var auth = await Factory.AuthManager.CreateAndLoginUserAsync("npc_user@example.com", "Password123!");
        Factory.ApiClient.SetAuth(auth);
        await Factory.AuthManager.CreateDefaultProfileAndSubmitLocation();

        // 2. Get Npc
        var npcResponse = await Factory.ApiClient.GetAsync<GetNpcResponse>(ApiRoutes.Npc.Root);
        npcResponse.ShouldNotBeNull();
        npcResponse.Npc.ShouldNotBeNull();
        npcResponse.Npc.Id.ShouldNotBeNullOrEmpty();
        npcResponse.Npc.Name.ShouldNotBeNullOrEmpty();

        // 3. Get Npc before timeout
        Factory.FakeTimeProvider.Advance(TimeSpan.FromHours(NpcActionsService.NpcMeetingTimeoutHours - 1));
        var npcResponseBeforeTimeout = await Factory.ApiClient
            .GetAsync<GetNpcResponse>(ApiRoutes.Npc.Root);
        npcResponseBeforeTimeout.ShouldNotBeNull();
        npcResponseBeforeTimeout.Npc.ShouldBeNull();

        // 3. Get Npc after timeout
        Factory.FakeTimeProvider.Advance(TimeSpan.FromHours(2));
        var npcResponseAfterTimeout = await Factory.ApiClient
            .GetAsync<GetNpcResponse>(ApiRoutes.Npc.Root);
        npcResponseAfterTimeout.ShouldNotBeNull();
        npcResponseAfterTimeout.Npc.ShouldNotBeNull();
        npcResponseAfterTimeout.Npc.Id.ShouldNotBeNullOrEmpty();
        npcResponseAfterTimeout.Npc.Name.ShouldNotBeNullOrEmpty();

        Factory.FakeTimeProvider.Reset(DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task GivenUser_WhenInviteNpc_ThenReturnsMonolog()
    {
        // 1. Create user
        var auth = await Factory.AuthManager.CreateAndLoginUserAsync("npc_inviter@example.com", "Password123!");
        Factory.ApiClient.SetAuth(auth);
        await Factory.AuthManager.CreateDefaultProfileAndSubmitLocation();

        // 2. Get Npc to get ID
        var npcResponse = await Factory.ApiClient.GetAsync<GetNpcResponse>(ApiRoutes.Npc.Root);
        npcResponse.Npc.ShouldNotBeNull();
        var npcId = npcResponse.Npc.Id;

        // 3. Invite Npc
        var inviteResponse = await Factory.ApiClient.GetAsync<InviteNpcResponse>(ApiRoutes.Npc.Invite(npcId));
        inviteResponse.Monolog.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task GivenUser_WhenInviteNpcTwice_ThenReturnsMonolog()
    {
        // 1. Create user
        var auth = await Factory.AuthManager.CreateAndLoginUserAsync("npc_inviter@example.com", "Password123!");
        Factory.ApiClient.SetAuth(auth);
        await Factory.AuthManager.CreateDefaultProfileAndSubmitLocation();

        // 2. Get Npc to get ID
        var npcResponse = await Factory.ApiClient.GetAsync<GetNpcResponse>(ApiRoutes.Npc.Root);
        npcResponse.Npc.ShouldNotBeNull();
        var npcId = npcResponse.Npc.Id;

        // 3. Invite Npc
        var inviteResponse1 = await Factory.ApiClient.GetAsync<InviteNpcResponse>(ApiRoutes.Npc.Invite(npcId));
        inviteResponse1.Monolog.ShouldNotBeNullOrEmpty();

        // 3. Invite Npc again
        var inviteResponse2 = await Factory.ApiClient.GetAsync<InviteNpcResponse>(ApiRoutes.Npc.Invite(npcId));
        inviteResponse2.Monolog.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task GivenUser_WhenNoNpcsInvited_ThenMetNpcsIsEmpty()
    {
        // 1. Create user
        var auth = await Factory.AuthManager.CreateAndLoginUserAsync("met_npc_empty@example.com", "Password123!");
        Factory.ApiClient.SetAuth(auth);
        await Factory.AuthManager.CreateDefaultProfileAndSubmitLocation();

        // 2. Get met NPCs — should be empty
        var response = await Factory.ApiClient.GetAsync<GetMetNpcsResponse>(ApiRoutes.Npc.MetNpcs);
        response.MetNpcs.ShouldNotBeNull();
        response.MetNpcs.ShouldBeEmpty();
    }

    [Fact]
    public async Task GivenUser_WhenInvitesNpc_ThenNpcAppearsInMetNpcs()
    {
        // 1. Create user
        var auth = await Factory.AuthManager.CreateAndLoginUserAsync("met_npc_user@example.com", "Password123!");
        Factory.ApiClient.SetAuth(auth);
        await Factory.AuthManager.CreateDefaultProfileAndSubmitLocation();

        // 2. Get Npc to get ID
        var npcResponse = await Factory.ApiClient.GetAsync<GetNpcResponse>(ApiRoutes.Npc.Root);
        npcResponse.Npc.ShouldNotBeNull();
        var npcId = npcResponse.Npc.Id;
        var npcName = npcResponse.Npc.Name;

        // 3. Invite NPC
        var inviteResponse = await Factory.ApiClient.GetAsync<InviteNpcResponse>(ApiRoutes.Npc.Invite(npcId));

        // 4. Get met NPCs — should contain the invited NPC
        var metResponse = await Factory.ApiClient.GetAsync<GetMetNpcsResponse>(ApiRoutes.Npc.MetNpcs);
        metResponse.MetNpcs.ShouldNotBeEmpty();
        metResponse.MetNpcs.Any(n => n.Id == npcId).ShouldBeTrue();
        metResponse.MetNpcs.First(n => n.Id == npcId).Name.ShouldBe(npcName);
    }

    [Fact]
    public async Task GivenUserWithEnglishLocale_WhenInviteNpc_ThenReturnsMonologInEnglish()
    {
        // 1. Create user
        var auth = await Factory.AuthManager.CreateAndLoginUserAsync("npc_inviter@example.com", "Password123!");
        Factory.ApiClient.SetAuth(auth);
        await Factory.AuthManager.CreateDefaultProfileAndSubmitLocation();

        // 2. Get Npc to get ID
        var npcResponse = await Factory.ApiClient.GetAsync<GetNpcResponse>(ApiRoutes.Npc.Root);
        npcResponse.Npc.ShouldNotBeNull();
        var npcId = npcResponse.Npc.Id;

        // 3. Set en locale and invite Npc
        Factory.ApiClient.SetLocale(SupportedLocales.English);
        var inviteResponse = await Factory.ApiClient.GetAsync<InviteNpcResponse>(ApiRoutes.Npc.Invite(npcId));
        // There is no letter "y" in the Serbian language, so we can just check for its presence.
        inviteResponse.Monolog.ShouldContain("y");
    }

    [Fact]
    public async Task GivenUserWithSerbianLocale_WhenInviteNpc_ThenReturnsMonologInSerbian()
    {
        // 1. Create user
        var auth = await Factory.AuthManager.CreateAndLoginUserAsync("npc_inviter@example.com", "Password123!");
        Factory.ApiClient.SetAuth(auth);
        await Factory.AuthManager.CreateDefaultProfileAndSubmitLocation();

        // 2. Get Npc to get ID
        var npcResponse = await Factory.ApiClient.GetAsync<GetNpcResponse>(ApiRoutes.Npc.Root);
        npcResponse.Npc.ShouldNotBeNull();
        var npcId = npcResponse.Npc.Id;

        // 3. Set en locale and invite Npc
        Factory.ApiClient.SetLocale(SupportedLocales.Serbian);
        var inviteResponse = await Factory.ApiClient.GetAsync<InviteNpcResponse>(ApiRoutes.Npc.Invite(npcId));
        inviteResponse.Monolog.ShouldContain(" je ");
    }
}
