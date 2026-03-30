using EngagementModule.API.Dto;
using EngagementModule.Domain.Repositories;
using EngagementModule.Domain.Services;
using IntegrationTests.Shared;
using Microsoft.Extensions.DependencyInjection;
using NotificationModule.Domain.ValueObjects;
using SharedCore.Api.Dto;
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
        var response = await Factory.ApiClient.GetAsync<ApiResult<GetNpcResponse>>(ApiRoutes.Npc.Root);
        response.Success.ShouldBeTrue();
        response.Result.ShouldNotBeNull();
        response.Result.Npc.ShouldNotBeNull();
        response.Result.Npc.Id.ShouldNotBeNullOrEmpty();
        response.Result.Npc.Name.ShouldNotBeNullOrEmpty();

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
        var npcResponse = await Factory.ApiClient.GetAsync<ApiResult<GetNpcResponse>>(ApiRoutes.Npc.Root);
        npcResponse.Success.ShouldBeTrue();
        npcResponse.Result.ShouldNotBeNull();
        npcResponse.Result.Npc.ShouldNotBeNull();
        npcResponse.Result.Npc.Id.ShouldNotBeNullOrEmpty();
        npcResponse.Result.Npc.Name.ShouldNotBeNullOrEmpty();

        // 3. Get Npc before timeout
        Factory.FakeTimeProvider.Advance(TimeSpan.FromHours(NpcActionsService.NpcMeetingTimeoutHours - 1));
        var npcResponseBeforeTimeout = await Factory.ApiClient
            .GetAsync<ApiResult<GetNpcResponse>>(ApiRoutes.Npc.Root);
        npcResponseBeforeTimeout.Success.ShouldBeTrue();
        npcResponseBeforeTimeout.Result.ShouldNotBeNull();
        npcResponseBeforeTimeout.Result.Npc.ShouldBeNull();

        // 3. Get Npc after timeout
        Factory.FakeTimeProvider.Advance(TimeSpan.FromHours(2));
        var npcResponseAfterTimeout = await Factory.ApiClient
            .GetAsync<ApiResult<GetNpcResponse>>(ApiRoutes.Npc.Root);
        npcResponseAfterTimeout.Success.ShouldBeTrue();
        npcResponseAfterTimeout.Result.ShouldNotBeNull();
        npcResponseAfterTimeout.Result.Npc.ShouldNotBeNull();
        npcResponseAfterTimeout.Result.Npc.Id.ShouldNotBeNullOrEmpty();
        npcResponseAfterTimeout.Result.Npc.Name.ShouldNotBeNullOrEmpty();

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
        var npcResponse = await Factory.ApiClient.GetAsync<ApiResult<GetNpcResponse>>(ApiRoutes.Npc.Root);
        npcResponse.Success.ShouldBeTrue();
        npcResponse.Result.Npc.ShouldNotBeNull();
        var npcId = npcResponse.Result.Npc.Id;

        // 3. Invite Npc
        var inviteResponse = await Factory.ApiClient.GetAsync<ApiResult<InviteNpcResponse>>(ApiRoutes.Npc.Invite(npcId));
        inviteResponse.Success.ShouldBeTrue();
        inviteResponse.Result.Monolog.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task GivenUser_WhenInviteNpcTwice_ThenReturnsMonolog()
    {
        // 1. Create user
        var auth = await Factory.AuthManager.CreateAndLoginUserAsync("npc_inviter@example.com", "Password123!");
        Factory.ApiClient.SetAuth(auth);
        await Factory.AuthManager.CreateDefaultProfileAndSubmitLocation();

        // 2. Get Npc to get ID
        var npcResponse = await Factory.ApiClient.GetAsync<ApiResult<GetNpcResponse>>(ApiRoutes.Npc.Root);
        npcResponse.Success.ShouldBeTrue();
        npcResponse.Result.Npc.ShouldNotBeNull();
        var npcId = npcResponse.Result.Npc.Id;

        // 3. Invite Npc
        var inviteResponse1 = await Factory.ApiClient.GetAsync<ApiResult<InviteNpcResponse>>(ApiRoutes.Npc.Invite(npcId));
        inviteResponse1.Success.ShouldBeTrue();
        inviteResponse1.Result.Monolog.ShouldNotBeNullOrEmpty();

        // 3. Invite Npc again
        var inviteResponse2 = await Factory.ApiClient.GetAsync<ApiResult<InviteNpcResponse>>(ApiRoutes.Npc.Invite(npcId));
        inviteResponse2.Success.ShouldBeTrue();
        inviteResponse2.Result.Monolog.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task GivenUser_WhenNoNpcsInvited_ThenMetNpcsIsEmpty()
    {
        // 1. Create user
        var auth = await Factory.AuthManager.CreateAndLoginUserAsync("met_npc_empty@example.com", "Password123!");
        Factory.ApiClient.SetAuth(auth);
        await Factory.AuthManager.CreateDefaultProfileAndSubmitLocation();

        // 2. Get met NPCs — should be empty
        var response = await Factory.ApiClient.GetAsync<ApiResult<GetMetNpcsResponse>>(ApiRoutes.Npc.MetNpcs);
        response.Success.ShouldBeTrue();
        response.Result.MetNpcs.ShouldNotBeNull();
        response.Result.MetNpcs.ShouldBeEmpty();
    }

    [Fact]
    public async Task GivenUser_WhenInvitesNpc_ThenNpcAppearsInMetNpcs()
    {
        // 1. Create user
        var auth = await Factory.AuthManager.CreateAndLoginUserAsync("met_npc_user@example.com", "Password123!");
        Factory.ApiClient.SetAuth(auth);
        await Factory.AuthManager.CreateDefaultProfileAndSubmitLocation();

        // 2. Get Npc to get ID
        var npcResponse = await Factory.ApiClient.GetAsync<ApiResult<GetNpcResponse>>(ApiRoutes.Npc.Root);
        npcResponse.Success.ShouldBeTrue();
        npcResponse.Result.Npc.ShouldNotBeNull();
        var npcId = npcResponse.Result.Npc.Id;
        var npcName = npcResponse.Result.Npc.Name;

        // 3. Invite NPC
        var inviteResponse = await Factory.ApiClient.GetAsync<ApiResult<InviteNpcResponse>>(ApiRoutes.Npc.Invite(npcId));
        inviteResponse.Success.ShouldBeTrue();

        // 4. Get met NPCs — should contain the invited NPC
        var metResponse = await Factory.ApiClient.GetAsync<ApiResult<GetMetNpcsResponse>>(ApiRoutes.Npc.MetNpcs);
        metResponse.Success.ShouldBeTrue();
        metResponse.Result.MetNpcs.ShouldNotBeEmpty();
        metResponse.Result.MetNpcs.Any(n => n.Id == npcId).ShouldBeTrue();
        metResponse.Result.MetNpcs.First(n => n.Id == npcId).Name.ShouldBe(npcName);
    }
    
    [Fact]
    public async Task GivenUserWithEnglishLocale_WhenInviteNpc_ThenReturnsMonologInEnglish()
    {
        // 1. Create user
        var auth = await Factory.AuthManager.CreateAndLoginUserAsync("npc_inviter@example.com", "Password123!");
        Factory.ApiClient.SetAuth(auth);
        await Factory.AuthManager.CreateDefaultProfileAndSubmitLocation();

        // 2. Get Npc to get ID
        var npcResponse = await Factory.ApiClient.GetAsync<ApiResult<GetNpcResponse>>(ApiRoutes.Npc.Root);
        npcResponse.Success.ShouldBeTrue();
        npcResponse.Result.Npc.ShouldNotBeNull();
        var npcId = npcResponse.Result.Npc.Id;

        // 3. Set en locale and invite Npc
        Factory.ApiClient.SetLocale(SupportedLocales.English);
        var inviteResponse = await Factory.ApiClient.GetAsync<ApiResult<InviteNpcResponse>>(ApiRoutes.Npc.Invite(npcId));
        inviteResponse.Success.ShouldBeTrue();
        inviteResponse.Result.Monolog.ShouldContain("beer");
    }
    
    [Fact]
    public async Task GivenUserWithSerbianLocale_WhenInviteNpc_ThenReturnsMonologInSerbian()
    {
        // 1. Create user
        var auth = await Factory.AuthManager.CreateAndLoginUserAsync("npc_inviter@example.com", "Password123!");
        Factory.ApiClient.SetAuth(auth);
        await Factory.AuthManager.CreateDefaultProfileAndSubmitLocation();

        // 2. Get Npc to get ID
        var npcResponse = await Factory.ApiClient.GetAsync<ApiResult<GetNpcResponse>>(ApiRoutes.Npc.Root);
        npcResponse.Success.ShouldBeTrue();
        npcResponse.Result.Npc.ShouldNotBeNull();
        var npcId = npcResponse.Result.Npc.Id;

        // 3. Set en locale and invite Npc
        Factory.ApiClient.SetLocale(SupportedLocales.Serbian);
        var inviteResponse = await Factory.ApiClient.GetAsync<ApiResult<InviteNpcResponse>>(ApiRoutes.Npc.Invite(npcId));
        inviteResponse.Success.ShouldBeTrue();
        inviteResponse.Result.Monolog.ShouldContain("pivo");
    }
}
