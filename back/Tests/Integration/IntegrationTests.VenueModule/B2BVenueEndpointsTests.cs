using System.Net.Http.Json;
using IntegrationTests.Shared;
using IntegrationTests.Shared.Extensions;
using SecurityModule.Domain.ValueObjects;
using SharedCore.Api.Dto;
using Shouldly;
using VenueModule.API.B2B.Companies.Dto;
using VenueModule.API.B2B.Venues.Dto;

namespace IntegrationTests.VenueModule;

[Collection("IntegrationTests")]
public class B2BVenueEndpointsTests : IntegrationTestBase, IAsyncLifetime
{
    public B2BVenueEndpointsTests(IntegrationWebApplicationFactory factory) : base(factory)
    {
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public Task DisposeAsync() => ContainerManager.ResetDatabaseAsync();

    [Fact]
    public async Task GivenAuthenticatedB2BUser_WhenCreatesVenue_ThenVenueCreated()
    {
        // 1. Sign in as B2B user and create company
        var authResponse = await Factory.AuthManager
            .CreateAndLoginUserAsync("b2b_venue@example.com", "Password123!", UserType.B2B);
        Factory.ApiClient.SetAuth(authResponse);

        var createCompanyRequest = new CreateCompanyRequest(Name: "Venue Owner Co", Email: "owner@test.com");
        var createCompanyResponse =
            await Factory.ApiClient.PostAsync<CreateCompanyRequest, ApiResult<CreateCompanyResponse>>(
                ApiRoutes.B2B.Company.Root, createCompanyRequest);
        var companyId = createCompanyResponse.Result.Id;

        // 2. Create venue
        var createVenueRequest = new CreateVenueRequest(
            Latitude: 40.7128,
            Longitude: -74.0060,
            Address: "123 Venue St",
            WebsiteUrl: "https://venue.com",
            GoogleMapsUrl: "https://maps.google.com/venue",
            InstagramUrl: "https://instagram.com/venue",
            PhoneNumber: "123456789",
            WorkingHours: "9:00 - 18:00",
            CoverImageUrl: TestConfig.S3.PublicUrl + "/cover.jpg");

        var createVenueResponse = await Factory.ApiClient.PostAsync<CreateVenueRequest, ApiResult<CreateVenueResponse>>(
            ApiRoutes.B2B.Company.Venues(companyId), createVenueRequest);

        createVenueResponse.Success.ShouldBeTrue();
        var venueId = createVenueResponse.Result.Id;

        // 3. Get venue and verify
        var getResponse = await Factory.ApiClient.GetAsync<ApiResult<GetVenueResponse>>(
            ApiRoutes.B2B.Venue.Single(venueId));

        getResponse.Success.ShouldBeTrue();
        getResponse.Result.Venue.ShouldSatisfyAllConditions(
            v => v.Id.ShouldBe(venueId.ToString()),
            v => v.Address.ShouldBe(createVenueRequest.Address),
            v => v.Latitude.ShouldBe(createVenueRequest.Latitude),
            v => v.Longitude.ShouldBe(createVenueRequest.Longitude)
        );
    }

    [Fact]
    public async Task GivenExistingVenue_WhenUserUpdatesVenue_ThenVenueUpdated()
    {
        // 1. Sign in as B2B user and create company/venue
        var authResponse =
            await Factory.AuthManager.CreateAndLoginUserAsync("b2b_venue_update@example.com", "Password123!",
                UserType.B2B);
        Factory.ApiClient.SetAuth(authResponse);

        var createCompanyResponse =
            await Factory.ApiClient.PostAsync<CreateCompanyRequest, ApiResult<CreateCompanyResponse>>(
                ApiRoutes.B2B.Company.Root, new CreateCompanyRequest("Update Co", "up@test.com"));
        var companyId = createCompanyResponse.Result.Id;

        var createVenueRequest = new CreateVenueRequest(40.0, -70.0, "Old Address", null, null, null, null, null, null);
        var createVenueResponse = await Factory.ApiClient.PostAsync<CreateVenueRequest, ApiResult<CreateVenueResponse>>(
            ApiRoutes.B2B.Company.Venues(companyId), createVenueRequest);
        var venueId = createVenueResponse.Result.Id;

        // 2. Update venue
        var patchRequest = new PatchVenueRequest(
            Latitude: 41.0,
            Longitude: -71.0,
            Address: "New Address",
            WebsiteUrl: "https://new.com",
            GoogleMapsUrl: null,
            InstagramUrl: null,
            PhoneNumber: null,
            WorkingHours: null,
            CoverImageUrl: TestConfig.S3.PublicUrl + "/new_cover.jpg",
            QrCode: null);

        var patchResponse = await Factory.ApiClient.PatchAsync<PatchVenueRequest, ApiResult.EmptyResult>(
            ApiRoutes.B2B.Venue.Single(venueId), patchRequest);
        patchResponse.Success.ShouldBeTrue();

        // 3. Get venue and verify updates
        var getResponse = await Factory.ApiClient.GetAsync<ApiResult<GetVenueResponse>>(
            ApiRoutes.B2B.Venue.Single(venueId));

        getResponse.Success.ShouldBeTrue();
        getResponse.Result.Venue.ShouldSatisfyAllConditions(
            v => v.Address.ShouldBe(patchRequest.Address),
            v => v.Latitude.ShouldBe(patchRequest.Latitude!.Value),
            v => v.Longitude.ShouldBe(patchRequest.Longitude!.Value),
            v => v.WebsiteUrl.ShouldBe(patchRequest.WebsiteUrl),
            v => v.CoverImageUrl!.ShouldBeNewerOrTheSameS3Url(patchRequest.CoverImageUrl!)
        );
    }

    [Fact]
    public async Task GivenExistingVenue_WhenUserDeletesVenue_ThenVenueDeleted()
    {
        // 1. Sign in as B2B user and create company/venue
        var authResponse =
            await Factory.AuthManager.CreateAndLoginUserAsync("b2b_venue_delete@example.com", "Password123!",
                UserType.B2B);
        Factory.ApiClient.SetAuth(authResponse);

        var createCompanyResponse =
            await Factory.ApiClient.PostAsync<CreateCompanyRequest, ApiResult<CreateCompanyResponse>>(
                ApiRoutes.B2B.Company.Root, new CreateCompanyRequest("Del Co", "del@test.com"));
        var companyId = createCompanyResponse.Result.Id;

        var createVenueResponse = await Factory.ApiClient
            .PostAsync<CreateVenueRequest, ApiResult<CreateVenueResponse>>(
                ApiRoutes.B2B.Company.Venues(companyId),
                new CreateVenueRequest(0, 0, "To Del", null, null, null, null, null, null));
        var venueId = createVenueResponse.Result.Id;

        // 2. Delete venue
        var deleteResponse = await Factory.ApiClient.DeleteAsync<ApiResult.EmptyResult>(
            ApiRoutes.B2B.Venue.Single(venueId));
        deleteResponse.Success.ShouldBeTrue();

        // 3. Verify venue is not found
        var getResponse = await Factory.ApiClient.GetAsync<ApiResult<GetVenueResponse>>(
            ApiRoutes.B2B.Venue.Single(venueId));
        getResponse.Success.ShouldBeFalse();
    }

    [Fact]
    public async Task GivenAuthenticatedB2BUser_WhenUserCreatesMultipleVenues_ThenVenuesReturnedInList()
    {
        // 1. Sign in as B2B user and create company
        var authResponse =
            await Factory.AuthManager.CreateAndLoginUserAsync("b2b_venue_list@example.com", "Password123!",
                UserType.B2B);
        Factory.ApiClient.SetAuth(authResponse);

        var createCompanyResponse =
            await Factory.ApiClient.PostAsync<CreateCompanyRequest, ApiResult<CreateCompanyResponse>>(
                ApiRoutes.B2B.Company.Root, new CreateCompanyRequest("List Co", "list@test.com"));
        var companyId = createCompanyResponse.Result.Id;

        // 2. Create 2 venues
        await Factory.ApiClient.PostAsync<CreateVenueRequest, ApiResult<CreateVenueResponse>>(
            ApiRoutes.B2B.Company.Venues(companyId),
            new CreateVenueRequest(1, 1, "Venue 1", null, null, null, null, null, null));
        await Factory.ApiClient.PostAsync<CreateVenueRequest, ApiResult<CreateVenueResponse>>(
            ApiRoutes.B2B.Company.Venues(companyId),
            new CreateVenueRequest(2, 2, "Venue 2", null, null, null, null, null, null));

        // 3. Get venues list and verify
        var listResponse = await Factory.ApiClient.GetAsync<ApiResult<GetVenuesResponse>>(
            ApiRoutes.B2B.Venue.Root);

        listResponse.Success.ShouldBeTrue();
        listResponse.Result.Venues.Length.ShouldBeGreaterThanOrEqualTo(2);
        listResponse.Result.Venues.ShouldContain(v => v.Address == "Venue 1");
        listResponse.Result.Venues.ShouldContain(v => v.Address == "Venue 2");
    }

    [Fact]
    public async Task GivenAuthenticatedB2CUser_WhenUserCreatesVenue_ThenReturnsForbidden()
    {
        // 1. Sign in as B2C user
        var authResponse = await Factory.AuthManager
            .CreateAndLoginUserAsync("b2c_intruder_v@example.com", "Password123!");
        Factory.ApiClient.SetAuth(authResponse);

        // 2. Try to create venue (need some company ID, even if non-existent or belonging to someone else)
        var dummyCompanyId = Guid.NewGuid();
        var createRequest = new CreateVenueRequest(0, 0, "I am B2C", null, null, null, null, null, null);

        var response =
            await Factory.HttpClient.PostAsJsonAsync(ApiRoutes.B2B.Company.Venues(dummyCompanyId), createRequest);

        // 3. Verify forbidden
        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.Forbidden);
    }
}