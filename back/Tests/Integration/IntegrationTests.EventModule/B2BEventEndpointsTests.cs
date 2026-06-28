using EventModule.API.B2B.Dto;
using EventModule.API.B2C.Dto;
using EventModule.Domain.Enums;
using IntegrationTests.Shared;
using SecurityModule.API.Dto;
using SecurityModule.Domain.ValueObjects;
using SharedCore.Api.Dto;
using SharedCore.Enums;
using SharedCore.Exceptions;
using Shouldly;
using VenueModule.API.B2B.Companies.Dto;
using VenueModule.API.B2B.Venues.Dto;

namespace IntegrationTests.EventModule;

[Collection("IntegrationTests")]
public class B2BEventEndpointsTests : IntegrationTestBase, IAsyncLifetime
{
    public B2BEventEndpointsTests(IntegrationWebApplicationFactory factory) : base(factory)
    {
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public Task DisposeAsync() => ContainerManager.ResetDatabaseAsync();

    // ========================================
    //  CREATE EVENT
    // ========================================

    [Fact]
    public async Task GivenB2BAdminWithVenue_WhenCreates_ThenCreated()
    {
        var (auth, companyId, venueId) = await CreateB2BCompanyAndVenueAsync();
        Factory.ApiClient.SetAuth(auth);

        var response = await Factory.ApiClient.PostAsync<B2BCreateEventRequest, ApiResult<EventDto>>(
            ApiRoutes.B2B.Event.Root,
            new B2BCreateEventRequest(
                CoverImageUrl: null,
                Title: "B2B Event",
                Description: "Business event",
                StartDateTime: DateTimeOffset.UtcNow.AddDays(30),
                VenueId: venueId));

        response.Success.ShouldBeTrue();
        response.Result.Type.ShouldBe(EventType.Event);
        response.Result.IsVisibleOnMap.ShouldBeTrue();
        response.Result.RequiresApproval.ShouldBeFalse();
        response.Result.MaxParticipants.ShouldBeNull();
        response.Result.Title.ShouldBe("B2B Event");
        response.Result.IsCreator.ShouldBeTrue();
    }

    [Fact]
    public async Task GivenB2BAdmin_WhenCreatesAtUnownedVenue_Then403()
    {
        var (ownerAuth, ownerCompanyId, ownerVenueId) = await CreateB2BCompanyAndVenueAsync();
        var otherAuth = await CreateB2BUserAsync();
        Factory.ApiClient.SetAuth(otherAuth);

        var response = await Factory.ApiClient.PostAsync<B2BCreateEventRequest, ApiResult<EventDto>>(
            ApiRoutes.B2B.Event.Root,
            new B2BCreateEventRequest(
                CoverImageUrl: null,
                Title: "Stolen Event",
                Description: "Desc",
                StartDateTime: DateTimeOffset.UtcNow.AddDays(30),
                VenueId: ownerVenueId));

        response.Success.ShouldBeFalse();
    }

    [Fact]
    public async Task GivenB2CUser_WhenCreatesB2B_Then403()
    {
        var b2cAuth = await CreateUserAsync();
        Factory.ApiClient.SetAuth(b2cAuth);

        await Should.ThrowAsync<HttpCodeException>(async () =>
        {
            await Factory.ApiClient.PostAsync<B2BCreateEventRequest, ApiResult<EventDto>>(
                ApiRoutes.B2B.Event.Root,
                new B2BCreateEventRequest(
                    CoverImageUrl: null,
                    Title: "Not Allowed",
                    Description: "Desc",
                    StartDateTime: DateTimeOffset.UtcNow.AddDays(30),
                    VenueId: Guid.NewGuid()),
                throwOnError:true);
        }); 
    }

    [Fact]
    public async Task GivenB2BAdmin_WhenCreatesAtMissingVenue_Then404()
    {
        var auth = await CreateB2BUserAsync();
        Factory.ApiClient.SetAuth(auth);

        var response = await Factory.ApiClient.PostAsync<B2BCreateEventRequest, ApiResult<EventDto>>(
            ApiRoutes.B2B.Event.Root,
            new B2BCreateEventRequest(
                CoverImageUrl: null,
                Title: "Missing Venue",
                Description: "Desc",
                StartDateTime: DateTimeOffset.UtcNow.AddDays(30),
                VenueId: Guid.NewGuid()));

        response.Success.ShouldBeFalse();
    }

    // ========================================
    //  LIST EVENTS
    // ========================================

    [Fact]
    public async Task GivenB2BUserWithEvents_WhenList_ThenPaginated()
    {
        var (auth, companyId, venueId) = await CreateB2BCompanyAndVenueAsync();
        Factory.ApiClient.SetAuth(auth);

        await Factory.ApiClient.PostAsync<B2BCreateEventRequest, ApiResult<EventDto>>(
            ApiRoutes.B2B.Event.Root,
            new B2BCreateEventRequest(
                CoverImageUrl: null,
                Title: "Event 1",
                Description: "Desc",
                StartDateTime: DateTimeOffset.UtcNow.AddDays(30),
                VenueId: venueId));

        await Factory.ApiClient.PostAsync<B2BCreateEventRequest, ApiResult<EventDto>>(
            ApiRoutes.B2B.Event.Root,
            new B2BCreateEventRequest(
                CoverImageUrl: null,
                Title: "Event 2",
                Description: "Desc",
                StartDateTime: DateTimeOffset.UtcNow.AddDays(60),
                VenueId: venueId));

        var response = await Factory.ApiClient.GetAsync<ApiResult<PaginatedResponse<B2BEventListItemDto>>>(
            ApiRoutes.B2B.Event.Root + "?Offset=0&Limit=20");

        response.Success.ShouldBeTrue();
        response.Result.Data.Count.ShouldBe(2);
        response.Result.TotalCount.ShouldBe(2);
        response.Result.Data.ShouldAllBe(e => e.VenueAddress == "Test Address");
    }

    [Fact]
    public async Task GivenB2BUser_WhenListFilteredByVenue_ThenFiltered()
    {
        var (auth, companyId, venueAId) = await CreateB2BCompanyAndVenueAsync();
        Factory.ApiClient.SetAuth(auth);

        await Factory.ApiClient.PostAsync<B2BCreateEventRequest, ApiResult<EventDto>>(
            ApiRoutes.B2B.Event.Root,
            new B2BCreateEventRequest(
                CoverImageUrl: null,
                Title: "Venue A Event",
                Description: "Desc",
                StartDateTime: DateTimeOffset.UtcNow.AddDays(30),
                VenueId: venueAId));

        var response = await Factory.ApiClient.GetAsync<ApiResult<PaginatedResponse<B2BEventListItemDto>>>(
            ApiRoutes.B2B.Event.Root + $"?VenueId={venueAId}&Offset=0&Limit=20");

        response.Success.ShouldBeTrue();
        response.Result.Data.Count.ShouldBe(1);
        response.Result.TotalCount.ShouldBe(1);
    }

    [Fact]
    public async Task GivenB2CUser_WhenListB2B_Then403()
    {
        var b2cAuth = await CreateUserAsync();
        Factory.ApiClient.SetAuth(b2cAuth);

        await Should.ThrowAsync<HttpCodeException>(async () =>
        {
            await Factory.ApiClient.GetAsync<ApiResult<PaginatedResponse<B2BEventListItemDto>>>(
                ApiRoutes.B2B.Event.Root + "?Offset=0&Limit=20", throwOnError: true);
        });
    }

    // ========================================
    //  GET EVENT DETAIL
    // ========================================

    [Fact]
    public async Task GivenCreator_WhenGetDetail_ThenReturnsEventDto()
    {
        var (auth, _, venueId) = await CreateB2BCompanyAndVenueAsync();
        Factory.ApiClient.SetAuth(auth);

        var createResponse = await Factory.ApiClient.PostAsync<B2BCreateEventRequest, ApiResult<EventDto>>(
            ApiRoutes.B2B.Event.Root,
            new B2BCreateEventRequest(
                CoverImageUrl: null,
                Title: "Detail Event",
                Description: "Detail Desc",
                StartDateTime: DateTimeOffset.UtcNow.AddDays(30),
                VenueId: venueId));
        var eventId = Guid.Parse(createResponse.Result.Id);

        var response = await Factory.ApiClient.GetAsync<ApiResult<EventDto>>(
            ApiRoutes.B2B.Event.Single(eventId));

        response.Success.ShouldBeTrue();
        response.Result.Title.ShouldBe("Detail Event");
        response.Result.IsCreator.ShouldBeTrue();
    }

    [Fact]
    public async Task GivenOtherB2BUser_WhenGetDetail_Then403()
    {
        var (creatorAuth, _, venueId) = await CreateB2BCompanyAndVenueAsync();
        Factory.ApiClient.SetAuth(creatorAuth);

        var createResponse = await Factory.ApiClient.PostAsync<B2BCreateEventRequest, ApiResult<EventDto>>(
            ApiRoutes.B2B.Event.Root,
            new B2BCreateEventRequest(
                CoverImageUrl: null,
                Title: "Private Event",
                Description: "Desc",
                StartDateTime: DateTimeOffset.UtcNow.AddDays(30),
                VenueId: venueId));
        var eventId = Guid.Parse(createResponse.Result.Id);

        var otherAuth = await CreateB2BUserAsync();
        Factory.ApiClient.SetAuth(otherAuth);

        var response = await Factory.ApiClient.GetAsync<ApiResult<EventDto>>(
            ApiRoutes.B2B.Event.Single(eventId));

        response.Success.ShouldBeFalse();
    }

    // ========================================
    //  UPDATE EVENT
    // ========================================

    [Fact]
    public async Task GivenCreator_WhenUpdate_ThenUpdated()
    {
        var (auth, _, venueId) = await CreateB2BCompanyAndVenueAsync();
        Factory.ApiClient.SetAuth(auth);

        var createResponse = await Factory.ApiClient.PostAsync<B2BCreateEventRequest, ApiResult<EventDto>>(
            ApiRoutes.B2B.Event.Root,
            new B2BCreateEventRequest(
                CoverImageUrl: null,
                Title: "Original Title",
                Description: "Original Desc",
                StartDateTime: DateTimeOffset.UtcNow.AddDays(30),
                VenueId: venueId));
        var eventId = Guid.Parse(createResponse.Result.Id);

        var response = await Factory.ApiClient.PatchAsync<B2BUpdateEventRequest, ApiResult<EventDto>>(
            ApiRoutes.B2B.Event.Single(eventId),
            new B2BUpdateEventRequest(
                Title: "Updated Title",
                Description: "Updated Desc",
                StartDateTime: null,
                CoverImageUrl: null,
                Contacts: null,
                HobbyIds: null));

        response.Success.ShouldBeTrue();
        response.Result.Title.ShouldBe("Updated Title");
        response.Result.Description.ShouldBe("Updated Desc");
    }

    [Fact]
    public async Task GivenNonCreator_WhenUpdate_Then403()
    {
        var (creatorAuth, _, venueId) = await CreateB2BCompanyAndVenueAsync();
        Factory.ApiClient.SetAuth(creatorAuth);

        var createResponse = await Factory.ApiClient.PostAsync<B2BCreateEventRequest, ApiResult<EventDto>>(
            ApiRoutes.B2B.Event.Root,
            new B2BCreateEventRequest(
                CoverImageUrl: null,
                Title: "Mine",
                Description: "Desc",
                StartDateTime: DateTimeOffset.UtcNow.AddDays(30),
                VenueId: venueId));
        var eventId = Guid.Parse(createResponse.Result.Id);

        var otherAuth = await CreateB2BUserAsync();
        Factory.ApiClient.SetAuth(otherAuth);

        var response = await Factory.ApiClient.PatchAsync<B2BUpdateEventRequest, ApiResult<EventDto>>(
            ApiRoutes.B2B.Event.Single(eventId),
            new B2BUpdateEventRequest(
                Title: "Hacked!",
                Description: null,
                StartDateTime: null,
                CoverImageUrl: null,
                Contacts: null,
                HobbyIds: null));

        response.Success.ShouldBeFalse();
    }

    // ========================================
    //  DELETE EVENT
    // ========================================

    [Fact]
    public async Task GivenCreator_WhenDelete_ThenSoftDeleted()
    {
        var (auth, _, venueId) = await CreateB2BCompanyAndVenueAsync();
        Factory.ApiClient.SetAuth(auth);

        var createResponse = await Factory.ApiClient.PostAsync<B2BCreateEventRequest, ApiResult<EventDto>>(
            ApiRoutes.B2B.Event.Root,
            new B2BCreateEventRequest(
                CoverImageUrl: null,
                Title: "Delete Me",
                Description: "Desc",
                StartDateTime: DateTimeOffset.UtcNow.AddDays(30),
                VenueId: venueId));
        var eventId = Guid.Parse(createResponse.Result.Id);

        var deleteResponse = await Factory.ApiClient.DeleteAsync<ApiResult.EmptyResult>(
            ApiRoutes.B2B.Event.Single(eventId));
        deleteResponse.Success.ShouldBeTrue();

        var getResponse = await Factory.ApiClient.GetAsync<ApiResult<EventDto>>(
            ApiRoutes.B2B.Event.Single(eventId));
        getResponse.Success.ShouldBeFalse();
    }

    [Fact]
    public async Task GivenDeletedB2BEvent_WhenGet_Then404()
    {
        var (auth, _, venueId) = await CreateB2BCompanyAndVenueAsync();
        Factory.ApiClient.SetAuth(auth);

        var createResponse = await Factory.ApiClient.PostAsync<B2BCreateEventRequest, ApiResult<EventDto>>(
            ApiRoutes.B2B.Event.Root,
            new B2BCreateEventRequest(
                CoverImageUrl: null,
                Title: "Gone",
                Description: "Desc",
                StartDateTime: DateTimeOffset.UtcNow.AddDays(30),
                VenueId: venueId));
        var eventId = Guid.Parse(createResponse.Result.Id);

        await Factory.ApiClient.DeleteAsync<ApiResult.EmptyResult>(
            ApiRoutes.B2B.Event.Single(eventId));

        var response = await Factory.ApiClient.GetAsync<ApiResult<EventDto>>(
            ApiRoutes.B2B.Event.Single(eventId));
        response.Success.ShouldBeFalse();
    }

    // ========================================
    //  COVER UPLOAD URL
    // ========================================

    [Fact]
    public async Task GivenCreator_WhenCoverUploadUrl_ThenReturnsUrls()
    {
        var (auth, _, venueId) = await CreateB2BCompanyAndVenueAsync();
        Factory.ApiClient.SetAuth(auth);

        var createResponse = await Factory.ApiClient.PostAsync<B2BCreateEventRequest, ApiResult<EventDto>>(
            ApiRoutes.B2B.Event.Root,
            new B2BCreateEventRequest(
                CoverImageUrl: null,
                Title: "Cover Event",
                Description: "Desc",
                StartDateTime: DateTimeOffset.UtcNow.AddDays(30),
                VenueId: venueId));
        var eventId = Guid.Parse(createResponse.Result.Id);

        var response = await Factory.ApiClient.PostAsync<GetPresignedUrlRequest, ApiResult<GetPresignedUrlResponse>>(
            ApiRoutes.B2B.Event.CoverUploadUrl(eventId),
            new GetPresignedUrlRequest(ContentType.ImageJpeg, 50000));

        response.Success.ShouldBeTrue();
        response.Result.UploadUrl.ShouldNotBeNullOrEmpty();
        response.Result.PublicDownloadUrl.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task GivenNonCreator_WhenCoverUploadUrl_Then403()
    {
        var (creatorAuth, _, venueId) = await CreateB2BCompanyAndVenueAsync();
        Factory.ApiClient.SetAuth(creatorAuth);

        var createResponse = await Factory.ApiClient.PostAsync<B2BCreateEventRequest, ApiResult<EventDto>>(
            ApiRoutes.B2B.Event.Root,
            new B2BCreateEventRequest(
                CoverImageUrl: null,
                Title: "Cover Event",
                Description: "Desc",
                StartDateTime: DateTimeOffset.UtcNow.AddDays(30),
                VenueId: venueId));
        var eventId = Guid.Parse(createResponse.Result.Id);

        var otherAuth = await CreateB2BUserAsync();
        Factory.ApiClient.SetAuth(otherAuth);

        await Should.ThrowAsync<HttpCodeException>(async () =>
        {
            await Factory.ApiClient.PostAsync<GetPresignedUrlRequest, ApiResult<GetPresignedUrlResponse>>(
                ApiRoutes.B2B.Event.CoverUploadUrl(eventId),
                new GetPresignedUrlRequest(ContentType.ImageJpeg, 50000),
                throwOnError: true);
        });
    }

    // ========================================
    //  HELPERS
    // ========================================

    private async Task<AuthResponse> CreateUserAsync()
    {
        return await Factory.AuthManager.CreateAndLoginUserAsync(
            $"b2c_{Guid.NewGuid():N}@hoppyfy.me", "Password123!", UserType.B2C);
    }

    private async Task<AuthResponse> CreateB2BUserAsync()
    {
        return await Factory.AuthManager.CreateAndLoginUserAsync(
            $"b2b_event_{Guid.NewGuid():N}@hoppyfy.me", "Password123!", UserType.B2B);
    }

    private async Task<(AuthResponse Auth, Guid CompanyId, Guid VenueId)> CreateB2BCompanyAndVenueAsync()
    {
        var auth = await CreateB2BUserAsync();
        Factory.ApiClient.SetAuth(auth);

        var companyResponse = await Factory.ApiClient.PostAsync<CreateCompanyRequest, ApiResult<CreateCompanyResponse>>(
            ApiRoutes.B2B.Company.Root,
            new CreateCompanyRequest(Name: "Event Co", Email: "event.co@test.com"));
        companyResponse.Success.ShouldBeTrue();
        var companyId = companyResponse.Result.Id;

        var venueResponse = await Factory.ApiClient.PostAsync<CreateVenueRequest, ApiResult<CreateVenueResponse>>(
            ApiRoutes.B2B.Company.Venues(companyId),
            new CreateVenueRequest(
                Latitude: 44.7866,
                Longitude: 20.4489,
                Address: "Test Address",
                WebsiteUrl: null,
                GoogleMapsUrl: null,
                InstagramUrl: null,
                PhoneNumber: null,
                WorkingHours: null,
                CoverImageUrl: null,
                TagIds: []));
        venueResponse.Success.ShouldBeTrue();
        var venueId = venueResponse.Result.Id;

        return (auth, companyId, venueId);
    }
}
