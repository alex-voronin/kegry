using IntegrationTests.Shared;
using IntegrationTests.Shared.Extensions;
using Microsoft.Extensions.DependencyInjection;
using SecurityModule.Domain.ValueObjects;
using SharedCore.Api.Dto;
using Shouldly;
using VenueModule.API.B2B.Companies.Dto;
using VenueModule.API.B2B.Venues.Dto;
using VenueModule.API.B2C.Dto;
using VenueModule.Domain.Repositories;
using VenueModule.Infrastructure.Persistence;
using GetVenueResponse = VenueModule.API.B2C.Dto.GetVenueResponse;
using VenueListItemDto = VenueModule.API.B2C.Dto.VenueListItemDto;

namespace IntegrationTests.VenueModule;

[Collection("IntegrationTests")]
public class B2CVenueEndpointsTests : IntegrationTestBase, IAsyncLifetime
{
    public B2CVenueEndpointsTests(IntegrationWebApplicationFactory factory) : base(factory)
    {
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public Task DisposeAsync() => ContainerManager.ResetDatabaseAsync();

    [Fact]
    public async Task GivenMultipleVerifiedVenues_WhenB2CUserListsVenues_ThenReturnsSortedByDistance()
    {
        // 1. Create B2B user
        var b2BAuth = await Factory.AuthManager
            .CreateAndLoginUserAsync("b2b_list@example.com", "Password123!", UserType.B2B);
        Factory.ApiClient.SetAuth(b2BAuth);

        var companyId = (await Factory.ApiClient.PostAsync<CreateCompanyRequest, ApiResult<CreateCompanyResponse>>(
            ApiRoutes.B2B.Company.Root, new CreateCompanyRequest("Sorting Co", "sort@example.com"))).Result.Id;

        // 2. Create venues
        // Venue 1: (5, 5)
        await CreateVerifiedVenueAsync(companyId, 0.5, 0.5, "Middle");
        // Venue 2: (1, 1) - closest to (0,0)
        await CreateVerifiedVenueAsync(companyId, 0.2, 0.2, "Closest");
        // Venue 3: (10, 10) - farthest
        await CreateVerifiedVenueAsync(companyId, 1.0, 1.0, "Farthest");

        // 3. Create B2C user with (0,0) location
        var b2CAuth = await Factory.AuthManager.CreateAndLoginUserAsync("b2c_list@example.com", "Password123!");
        Factory.ApiClient.SetAuth(b2CAuth);

        // 4. Get venues list and check order
        var listRequest = new GetVenuesRequest(0, 0, 0, 10);
        var response =
            await Factory.ApiClient.GetAsync<GetVenuesRequest, ApiResult<PaginatedResponse<VenueListItemDto>>>(
                ApiRoutes.B2C.Venue.Root, listRequest);

        response.Success.ShouldBeTrue();
        response.Result.TotalCount.ShouldBeGreaterThanOrEqualTo(3);
        var venues = response.Result.Data.ToArray();
        venues[0].Address.ShouldBe("Closest");
        venues[1].Address.ShouldBe("Middle");
        venues[2].Address.ShouldBe("Farthest");
        venues[0].DistanceMeters.ShouldBeLessThan(venues[1].DistanceMeters);
        venues[1].DistanceMeters.ShouldBeLessThan(venues[2].DistanceMeters);
    }

    [Fact]
    public async Task GivenNonVerifiedVenue_WhenB2CUserListsVenues_ThenNotReturned()
    {
        // 1. Create B2B user
        var b2BAuth =
            await Factory.AuthManager.CreateAndLoginUserAsync("b2b_visibility@example.com", "Password123!",
                UserType.B2B);
        Factory.ApiClient.SetAuth(b2BAuth);

        var companyId = (await Factory.ApiClient.PostAsync<CreateCompanyRequest, ApiResult<CreateCompanyResponse>>(
            ApiRoutes.B2B.Company.Root, new CreateCompanyRequest("Visibility Co", "vis@example.com"))).Result.Id;

        // 2. Create verified and non-verified venue
        await CreateVerifiedVenueAsync(companyId, 0.2, 0.2, "Verified Venue");
        // Non-verified venue
        await Factory.ApiClient.PostAsync<CreateVenueRequest, ApiResult<CreateVenueResponse>>(
            ApiRoutes.B2B.Company.Venues(companyId),
            new CreateVenueRequest(0.4, 0.4, "Non-Verified Venue", null, null, null, null, null, null));

        // 3. Create B2C user
        var b2CAuth = await Factory.AuthManager.CreateAndLoginUserAsync("b2c_visibility@example.com", "Password123!");
        Factory.ApiClient.SetAuth(b2CAuth);

        // 4. Get venues list and ensure only verified venues are present.
        var response =
            await Factory.ApiClient.GetAsync<GetVenuesRequest, ApiResult<PaginatedResponse<VenueListItemDto>>>(
                ApiRoutes.B2C.Venue.Root, new GetVenuesRequest(0, 0, 0, 10));

        response.Result.Data.ShouldContain(v => v.Address == "Verified Venue");
        response.Result.Data.ShouldNotContain(v => v.Address == "Non-Verified Venue");
    }

    [Fact]
    public async Task GivenVenue_WhenB2CUserGetsDetail_ThenReturnsValidData()
    {
        // 1. Create B2B user
        var b2BAuth = await Factory.AuthManager
            .CreateAndLoginUserAsync("b2b_detail@example.com", "Password123!", UserType.B2B);
        Factory.ApiClient.SetAuth(b2BAuth);

        // 2. Create company and venue
        var companyId = (await Factory.ApiClient.PostAsync<CreateCompanyRequest, ApiResult<CreateCompanyResponse>>(
            ApiRoutes.B2B.Company.Root, new CreateCompanyRequest("Detail Co", "det@example.com"))).Result.Id;

        var venueRequest = new CreateVenueRequest(45, 13, "Detail Venue", "https://site.com", "https://google.com/mo",
            "https://instagram.com/some-venue", "123", "9-5", TestConfig.S3.PublicUrl + "/cover.jpg");
        var venueResponse = await Factory.ApiClient.PostAsync<CreateVenueRequest, ApiResult<CreateVenueResponse>>(
            ApiRoutes.B2B.Company.Venues(companyId), venueRequest);
        venueResponse.Success.ShouldBeTrue();
        var venueId = venueResponse.Result.Id;

        // 3. Create B2C user
        var b2CAuth = await Factory.AuthManager.CreateAndLoginUserAsync("b2c_detail@example.com", "Password123!");
        Factory.ApiClient.SetAuth(b2CAuth);

        // 4. Get venues and check data
        var response = await Factory.ApiClient.GetAsync<ApiResult<GetVenueResponse>>(
            ApiRoutes.B2C.Venue.Single(venueId));
        
        response.Success.ShouldBeTrue();
        var venue = response.Result.Venue;
        venue.Address.ShouldBe(venueRequest.Address);
        venue.WebsiteUrl.ShouldBe(venueRequest.WebsiteUrl);
        venue.InstagramUrl.ShouldBe(venueRequest.InstagramUrl);
        venue.CoverImageUrl!.ShouldBeNewerOrTheSameS3Url(venueRequest.CoverImageUrl!);
    }

    private async Task CreateVerifiedVenueAsync(Guid companyId, double lat, double lon, string name)
    {
        var createVenueResponse = await Factory.ApiClient
            .PostAsync<CreateVenueRequest, ApiResult<CreateVenueResponse>>(
                ApiRoutes.B2B.Company.Venues(companyId),
                new CreateVenueRequest(lat, lon, name, null, null, null, null, null, null));

        var venueId = createVenueResponse.Result.Id;
        await VerifyVenueByIdAsync(venueId);
    }

    private async Task VerifyVenueByIdAsync(Guid venueId)
    {
        using var scope = Factory.Services.CreateScope();
        var venueRepository = scope.ServiceProvider.GetService<IVenueRepository>()!;
        var v = await venueRepository.GetByIdAsync(venueId, ignoreFilters: [NamedFilters.Venue.Verified]);
        v!.IsVerified = true;
        await venueRepository.SaveChangesAsync();
    }
}