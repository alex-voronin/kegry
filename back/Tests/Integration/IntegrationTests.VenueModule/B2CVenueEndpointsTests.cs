using IntegrationTests.Shared;
using IntegrationTests.Shared.Extensions;
using SecurityModule.Domain.ValueObjects;
using SharedCore.Api.Dto;
using SharedCore.Exceptions;
using Shouldly;
using VenueModule.API.B2B.Companies.Dto;
using VenueModule.API.B2B.Deals.Dto;
using VenueModule.API.B2B.Venues.Dto;
using VenueModule.API.B2C.Dto;
using VenueModule.Domain.ValueObjects;
using B2BGetVenueResponse = VenueModule.API.B2B.Venues.Dto.GetVenueResponse;
using GetVenueResponse = VenueModule.API.B2C.Dto.GetVenueResponse;
using VenueListItemDto = VenueModule.API.B2C.Dto.VenueListItemDto;

namespace IntegrationTests.VenueModule;

[Collection("IntegrationTests")]
public class B2CVenueEndpointsTests : IntegrationTestBase, IAsyncLifetime
{
    private readonly TestUtils _testUtils;

    public B2CVenueEndpointsTests(IntegrationWebApplicationFactory factory) : base(factory)
    {
        _testUtils = new TestUtils(factory);
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
        await _testUtils.CreateVerifiedVenueAsync(companyId, 0.1, 0.1, "Middle");
        await _testUtils.CreateVerifiedVenueAsync(companyId, 0.05, 0.05, "Closest");
        await _testUtils.CreateVerifiedVenueAsync(companyId, 0.15, 0.15, "Farthest");

        // 3. Create B2C user with (0,0) location
        var b2CAuth = await Factory.AuthManager.CreateAndLoginUserAsync("b2c_list@example.com", "Password123!");
        Factory.ApiClient.SetAuth(b2CAuth);

        // 4. Get venues list and check order
        var listRequest = new GetVenuesRequest(0, 0, 0, 10);
        var response =
            await Factory.ApiClient.GetAsync<GetVenuesRequest, ApiResult<PaginatedResponse<VenueListItemDto>>>(
                ApiRoutes.Venue.Root, listRequest);

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
        await _testUtils.CreateVerifiedVenueAsync(companyId, 0.05, 0.05, "Verified Venue");
        // Non-verified venue
        await Factory.ApiClient.PostAsync<CreateVenueRequest, ApiResult<CreateVenueResponse>>(
            ApiRoutes.B2B.Company.Venues(companyId),
            new CreateVenueRequest(0.1, 0.1, "Non-Verified Venue", null, null, null, null, null, null, null));

        // 3. Create B2C user
        var b2CAuth = await Factory.AuthManager.CreateAndLoginUserAsync("b2c_visibility@example.com", "Password123!");
        Factory.ApiClient.SetAuth(b2CAuth);

        // 4. Get venues list and ensure only verified venues are present.
        var response =
            await Factory.ApiClient.GetAsync<GetVenuesRequest, ApiResult<PaginatedResponse<VenueListItemDto>>>(
                ApiRoutes.Venue.Root, new GetVenuesRequest(0, 0, 0, 10));

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
            "https://instagram.com/some-venue", "123", "9-5", TestConfig.S3.PublicUrl + "/cover.jpg", null);
        var venueResponse = await Factory.ApiClient.PostAsync<CreateVenueRequest, ApiResult<CreateVenueResponse>>(
            ApiRoutes.B2B.Company.Venues(companyId), venueRequest);
        venueResponse.Success.ShouldBeTrue();
        var venueId = venueResponse.Result.Id;

        // 3. Create B2C user
        var b2CAuth = await Factory.AuthManager.CreateAndLoginUserAsync("b2c_detail@example.com", "Password123!");
        Factory.ApiClient.SetAuth(b2CAuth);

        // 4. Get venues and check data
        var response = await Factory.ApiClient.GetAsync<ApiResult<GetVenueResponse>>(
            ApiRoutes.Venue.Single(venueId));

        response.Success.ShouldBeTrue();
        var venue = response.Result.Venue;
        venue.QrCode.ShouldBe(venue.Id);
        venue.IsPinCheckInAllowed.ShouldBe(false);
        venue.Address.ShouldBe(venueRequest.Address);
        venue.WebsiteUrl.ShouldBe(venueRequest.WebsiteUrl);
        venue.InstagramUrl.ShouldBe(venueRequest.InstagramUrl);
        venue.CoverImageUrl!.ShouldBeNewerOrTheSameS3Url(venueRequest.CoverImageUrl!);
    }

    [Fact]
    public async Task GivenVenueWithMultipleDiscounts_WhenB2CUserGetsDiscounts_ThenReturnsCorrectForDayAndLevel()
    {
        // 1. Create B2B user
        var b2BAuth = await Factory.AuthManager
            .CreateAndLoginUserAsync("b2b_deals@example.com", "Password123!", UserType.B2B);
        Factory.ApiClient.SetAuth(b2BAuth);

        // 2. Create company and venue
        var createCompanyRequest = new CreateCompanyRequest("Deals Co", "deals@example.com");
        var createCompanyResponse = await Factory.ApiClient
            .PostAsync<CreateCompanyRequest, ApiResult<CreateCompanyResponse>>(ApiRoutes.B2B.Company.Root,
                createCompanyRequest);
        createCompanyResponse.Success.ShouldBeTrue();
        var companyId = createCompanyResponse.Result.Id;

        var createVenueRequest = new CreateVenueRequest(0, 0, "Deals Venue", null, null, null, null, null, null, null);
        var createVenueResponse = await Factory.ApiClient.PostAsync<CreateVenueRequest, ApiResult<CreateVenueResponse>>(
            ApiRoutes.B2B.Company.Venues(companyId), createVenueRequest);
        createVenueResponse.Success.ShouldBeTrue();
        var venueId = createVenueResponse.Result.Id;

        // 3. Enable 0 loyalty level
        await Factory.ApiClient.PutAsync<UpdateLoyaltyLevelRequest, ApiResult.EmptyResult>(
            ApiRoutes.B2B.Company.LoyaltyLevels(companyId),
            new UpdateLoyaltyLevelRequest(new LoyaltyLevelDto(0, 1, true)));

        // 3. Create discounts
        var loyaltyLevelsResponse = await Factory.ApiClient.GetAsync<ApiResult<LoyaltyLevelDto[]>>(
            ApiRoutes.B2B.Company.LoyaltyLevels(companyId));
        loyaltyLevelsResponse.Success.ShouldBeTrue();
        var loyaltyLevels = loyaltyLevelsResponse.Result;
        var levelId = loyaltyLevels[0].Id;

        // Discount for Monday (10%)
        await Factory.ApiClient.PostAsync<CreateDiscountRequest, ApiResult<CreateDiscountResponse>>(
            ApiRoutes.B2B.Venue.Discounts(venueId), new CreateDiscountRequest(levelId, 10, [DayOfWeek.Monday]));

        // Discount for Friday (20%)
        await Factory.ApiClient.PostAsync<CreateDiscountRequest, ApiResult<CreateDiscountResponse>>(
            ApiRoutes.B2B.Venue.Discounts(venueId), new CreateDiscountRequest(levelId, 20, [DayOfWeek.Friday]));

        // 4. Create B2C user and check discounts
        var b2CAuth = await Factory.AuthManager.CreateAndLoginUserAsync("b2c_deals@example.com", "Password123!");
        Factory.ApiClient.SetAuth(b2CAuth);

        var response = await Factory.ApiClient.GetAsync<ApiResult<GetDiscountsByWeekdayResponse>>(
            ApiRoutes.Venue.Discounts(venueId));

        response.Result.Monday.CurrentLevelDiscount.ShouldBe(10);
        response.Result.Friday.CurrentLevelDiscount.ShouldBe(20);
        response.Result.Tuesday.CurrentLevelDiscount.ShouldBe(0);
    }

    [Fact]
    public async Task GivenVenueWithTieredDiscounts_WhenB2CUserChecksInOverTime_ThenCorrectDiscountAppliedAtEachTier()
    {
        // 1. Setup: B2B user creates company/venue
        var b2BAuth =
            await Factory.AuthManager.CreateAndLoginUserAsync("b2b_complex@example.com", "Password123!", UserType.B2B);
        Factory.ApiClient.SetAuth(b2BAuth);

        var createCompanyRequest = new CreateCompanyRequest("Deals Co", "deals@example.com");
        var createCompanyResponse = await Factory.ApiClient
            .PostAsync<CreateCompanyRequest, ApiResult<CreateCompanyResponse>>(ApiRoutes.B2B.Company.Root,
                createCompanyRequest);
        createCompanyResponse.Success.ShouldBeTrue();
        var companyId = createCompanyResponse.Result.Id;

        var createVenueRequest = new CreateVenueRequest(0, 0, "Deals Venue", null, null, null, null, null, null, null);
        var createVenueResponse = await Factory.ApiClient.PostAsync<CreateVenueRequest, ApiResult<CreateVenueResponse>>(
            ApiRoutes.B2B.Company.Venues(companyId), createVenueRequest);
        createVenueResponse.Success.ShouldBeTrue();
        var venueId = createVenueResponse.Result.Id;

        // 2. Configure Loyalty Levels: 0 (2 visits), 1 (4 visits), 2 (10 visits)
        var levels = new[]
        {
            new LoyaltyLevelDto(0, 2, true),
            new LoyaltyLevelDto(1, 4, true),
            new LoyaltyLevelDto(2, 10, true),
            new LoyaltyLevelDto(3, 100, false)
        };

        foreach (var level in levels)
        {
            await Factory.ApiClient.PutAsync<UpdateLoyaltyLevelRequest, ApiResult.EmptyResult>(
                ApiRoutes.B2B.Company.LoyaltyLevels(companyId),
                new UpdateLoyaltyLevelRequest(level));
        }

        // 3. Create Discounts (0%, 5%, 10% for levels 0, 1, 2) except Friday
        var workDaysExceptFriday = Enum.GetValues<DayOfWeek>().Where(d => d != DayOfWeek.Friday).ToArray();
        var discounts = new[]
        {
            new CreateDiscountRequest(0, 0, workDaysExceptFriday),
            new CreateDiscountRequest(1, 5, workDaysExceptFriday),
            new CreateDiscountRequest(2, 10, workDaysExceptFriday)
        };

        foreach (var discount in discounts)
        {
            await Factory.ApiClient.PostAsync<CreateDiscountRequest, ApiResult<CreateDiscountResponse>>(
                ApiRoutes.B2B.Venue.Discounts(venueId), discount);
        }

        // 4. Create B2C User
        var b2CAuth = await Factory.AuthManager.CreateAndLoginUserAsync("b2c_complex@example.com", "Password123!");
        Factory.ApiClient.SetAuth(b2CAuth);

        // 5. Run loop of check-ins
        var currentTime = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
        Factory.FakeTimeProvider.Reset(currentTime);

        for (var visits = 0; visits <= levels[2].VisitsUpperBound + 1; visits++)
        {
            var now = Factory.FakeTimeProvider.GetUtcNow();
            var day = now.DayOfWeek;

            // Verify current discount
            var response = await Factory.ApiClient.GetAsync<ApiResult<GetVenueResponse>>(
                ApiRoutes.Venue.Single(venueId));
            response.Success.ShouldBeTrue();

            var expectedDiscount = 0;
            if (day != DayOfWeek.Friday)
            {
                expectedDiscount = visits switch
                {
                    < 2 => 0,
                    < 4 => 5,
                    _ => 10
                };
            }

            response.Result.LoyaltyLevel.Discount.ShouldBe(expectedDiscount, $"Visits: {visits}, Day: {day}");

            // Perform Check-in
            var checkInResponse = await Factory.ApiClient.PostAsync<CheckInRequest, ApiResult<CheckInResponse>>(
                ApiRoutes.Venue.QrCheckIn(venueId), new CheckInRequest(0, 0, null));
            checkInResponse.Success.ShouldBeTrue();

            // Advance time by 1 day
            currentTime = currentTime.AddDays(1);
            Factory.FakeTimeProvider.SetUtcNow(currentTime);
        }

        Factory.FakeTimeProvider.Reset(DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task GivenVenue_WhenB2CUserChecksInInRange_ThenCheckInSuccessful()
    {
        // 1. Setup: B2B user creates verified venue
        var b2BAuth =
            await Factory.AuthManager.CreateAndLoginUserAsync("b2b_checkin@example.com", "Password123!", UserType.B2B);
        Factory.ApiClient.SetAuth(b2BAuth);

        var companyId = (await Factory.ApiClient.PostAsync<CreateCompanyRequest, ApiResult<CreateCompanyResponse>>(
            ApiRoutes.B2B.Company.Root, new CreateCompanyRequest("Checkin Co", "chk@example.com"))).Result.Id;

        var venueId = (await Factory.ApiClient.PostAsync<CreateVenueRequest, ApiResult<CreateVenueResponse>>(
                ApiRoutes.B2B.Company.Venues(companyId),
                new CreateVenueRequest(50, 50, "Checkin Venue", null, null, null, null, null, null, null)))
            .Result
            .Id;

        await _testUtils.VerifyVenueByIdAsync(venueId);

        // 2. Create B2C user
        var b2CAuth = await Factory.AuthManager.CreateAndLoginUserAsync("b2c_checkin@example.com", "Password123!");
        Factory.ApiClient.SetAuth(b2CAuth);

        // 3. Check in as B2C user
        var checkInRequest = new CheckInRequest(50.00001, 50.00001, null);
        var response = await Factory.ApiClient.PostAsync<CheckInRequest, ApiResult<CheckInResponse>>(
            ApiRoutes.Venue.QrCheckIn(venueId), checkInRequest);

        response.Success.ShouldBeTrue();
        response.Result.VenueId.ShouldBe(venueId);
    }

    [Fact]
    public async Task GivenVenue_WhenB2CUserChecksInOutOfRange_ThenCheckInFailed()
    {
        // 1. Setup: B2B user creates verified venue
        var b2BAuth =
            await Factory.AuthManager.CreateAndLoginUserAsync("b2b_checkin@example.com", "Password123!", UserType.B2B);
        Factory.ApiClient.SetAuth(b2BAuth);

        var companyId = (await Factory.ApiClient.PostAsync<CreateCompanyRequest, ApiResult<CreateCompanyResponse>>(
            ApiRoutes.B2B.Company.Root, new CreateCompanyRequest("Checkin Co", "chk@example.com"))).Result.Id;

        var venueId = (await Factory.ApiClient.PostAsync<CreateVenueRequest, ApiResult<CreateVenueResponse>>(
                ApiRoutes.B2B.Company.Venues(companyId),
                new CreateVenueRequest(50, 50, "Checkin Venue", null, null, null, null, null, null, null)))
            .Result
            .Id;

        await _testUtils.VerifyVenueByIdAsync(venueId);

        // 2. Create B2C user
        var b2CAuth = await Factory.AuthManager.CreateAndLoginUserAsync("b2c_checkin@example.com", "Password123!");
        Factory.ApiClient.SetAuth(b2CAuth);

        // 3. Check in as B2C user
        var checkInRequest = new CheckInRequest(51, 51, null);
        var response = await Factory.ApiClient.PostAsync<CheckInRequest, ApiResult<CheckInResponse>>(
            ApiRoutes.Venue.QrCheckIn(venueId), checkInRequest);

        response.Success.ShouldBeFalse();
        response.Errors[0].Code.ShouldBe(nameof(ErrorCode.ValidationError));
    }

    [Fact]
    public async Task GivenVenue_WhenB2BChangesQrCode_ThenCheckInOnlyWithNewOne()
    {
        // 1. Create B2B user
        var b2BAuth = await Factory.AuthManager
            .CreateAndLoginUserAsync("b2b_qrchange@example.com", "Password123!", UserType.B2B);
        Factory.ApiClient.SetAuth(b2BAuth);

        // 2. Create company and venue
        var createCompanyRequest = new CreateCompanyRequest("QrChange Co", "qr@example.com");
        var createCompanyResponse = await Factory.ApiClient
            .PostAsync<CreateCompanyRequest, ApiResult<CreateCompanyResponse>>(ApiRoutes.B2B.Company.Root,
                createCompanyRequest);
        var companyId = createCompanyResponse.Result.Id;

        var createVenueResponse = await Factory.ApiClient
            .PostAsync<CreateVenueRequest, ApiResult<CreateVenueResponse>>(
                ApiRoutes.B2B.Company.Venues(companyId),
                new CreateVenueRequest(0, 0, "Qr Venue", null, null, null, null, null, null, null));
        var venueId = createVenueResponse.Result.Id;

        // 3. Update QR Code
        var newQr = Guid.CreateVersion7();
        var patchResponse = await Factory.ApiClient.PatchAsync<PatchVenueRequest, ApiResult.EmptyResult>(
            ApiRoutes.B2B.Venue.Single(venueId),
            new PatchVenueRequest(
                Latitude: null,
                Longitude: null,
                Address: null,
                WebsiteUrl: null,
                GoogleMapsUrl: null,
                InstagramUrl: null,
                PhoneNumber: null,
                WorkingHours: null,
                CoverImageUrl: null,
                PinCode: null,
                IsPinCheckInAllowed: null,
                QrCode: newQr,
                TagIds: null
            )
        );
        patchResponse.Success.ShouldBeTrue();

        // 4. Check in with the old QR
        var b2CAuth = await Factory.AuthManager.CreateAndLoginUserAsync("b2c_qr@example.com", "Password123!");
        Factory.ApiClient.SetAuth(b2CAuth);

        var oldResponse = await Factory.ApiClient
            .PostAsync<CheckInRequest, ApiResult<CheckInResponse>>(
                ApiRoutes.Venue.QrCheckIn(venueId),
                new CheckInRequest(0, 0, null)
            );
        oldResponse.Success.ShouldBeFalse();

        // 5. Check in with the new QR
        var newResponse = await Factory.ApiClient
            .PostAsync<CheckInRequest, ApiResult<CheckInResponse>>(
                ApiRoutes.Venue.QrCheckIn(newQr),
                new CheckInRequest(0, 0, null)
            );
        newResponse.Success.ShouldBeTrue();
        newResponse.Result.VenueId.ShouldBe(venueId);
    }

    [Fact]
    public async Task GivenVenue_WhenB2CUserChecksInWithDailyPinCode_ThenCheckInSuccessful()
    {
        // 1. Setup: B2B user creates verified venue
        var b2BAuth =
            await Factory.AuthManager.CreateAndLoginUserAsync("b2b_checkin@example.com", "Password123!", UserType.B2B);
        Factory.ApiClient.SetAuth(b2BAuth);

        var companyId = (await Factory.ApiClient.PostAsync<CreateCompanyRequest, ApiResult<CreateCompanyResponse>>(
            ApiRoutes.B2B.Company.Root, new CreateCompanyRequest("Checkin Co", "chk@example.com"))).Result.Id;

        var venueId = (await Factory.ApiClient.PostAsync<CreateVenueRequest, ApiResult<CreateVenueResponse>>(
                ApiRoutes.B2B.Company.Venues(companyId),
                new CreateVenueRequest(50, 50, "Checkin Venue", null, null, null, null, null, null, null)))
            .Result
            .Id;
        await _testUtils.VerifyVenueByIdAsync(venueId);

        // allow pin-code check-in
        var patchResponse = await Factory.ApiClient.PatchAsync<PatchVenueRequest, ApiResult.EmptyResult>(
            ApiRoutes.B2B.Venue.Single(venueId),
            new PatchVenueRequest(
                Latitude: null,
                Longitude: null,
                Address: null,
                WebsiteUrl: null,
                GoogleMapsUrl: null,
                InstagramUrl: null,
                PhoneNumber: null,
                WorkingHours: null,
                CoverImageUrl: null,
                PinCode: null,
                IsPinCheckInAllowed: true,
                QrCode: null,
                TagIds: null
            )
        );
        patchResponse.Success.ShouldBeTrue();

        var getVenueResponse = await Factory.ApiClient.GetAsync<ApiResult<B2BGetVenueResponse>>(
            ApiRoutes.B2B.Venue.Single(venueId));
        getVenueResponse.Success.ShouldBeTrue();

        // 2. Create B2C user
        var b2CAuth = await Factory.AuthManager.CreateAndLoginUserAsync("b2c_checkin@example.com", "Password123!");
        Factory.ApiClient.SetAuth(b2CAuth);

        // 3. Check in as B2C user
        var checkInRequest = new CheckInRequest(null, null, getVenueResponse.Result.Venue.PinCode);
        var response = await Factory.ApiClient.PostAsync<CheckInRequest, ApiResult<CheckInResponse>>(
            ApiRoutes.Venue.QrCheckIn(venueId), checkInRequest);

        response.Success.ShouldBeTrue();
        response.Result.VenueId.ShouldBe(venueId);
    }

    [Fact]
    public async Task GivenVenue_WhenB2CUserChecksInWithCorrectPinCode_ThenCheckInSuccessful()
    {
        // 1. Setup: B2B user creates verified venue
        var b2BAuth =
            await Factory.AuthManager.CreateAndLoginUserAsync("b2b_checkin@example.com", "Password123!", UserType.B2B);
        Factory.ApiClient.SetAuth(b2BAuth);

        var companyId = (await Factory.ApiClient.PostAsync<CreateCompanyRequest, ApiResult<CreateCompanyResponse>>(
            ApiRoutes.B2B.Company.Root, new CreateCompanyRequest("Checkin Co", "chk@example.com"))).Result.Id;

        var venueId = (await Factory.ApiClient.PostAsync<CreateVenueRequest, ApiResult<CreateVenueResponse>>(
                ApiRoutes.B2B.Company.Venues(companyId),
                new CreateVenueRequest(50, 50, "Checkin Venue", null, null, null, null, null, null, null)))
            .Result
            .Id;

        var newPinCode = "1234";
        var patchResponse = await Factory.ApiClient.PatchAsync<PatchVenueRequest, ApiResult.EmptyResult>(
            ApiRoutes.B2B.Venue.Single(venueId),
            new PatchVenueRequest(
                Latitude: null,
                Longitude: null,
                Address: null,
                WebsiteUrl: null,
                GoogleMapsUrl: null,
                InstagramUrl: null,
                PhoneNumber: null,
                WorkingHours: null,
                CoverImageUrl: null,
                PinCode: newPinCode,
                IsPinCheckInAllowed: true,
                QrCode: null,
                TagIds: null
            )
        );
        patchResponse.Success.ShouldBeTrue();

        await _testUtils.VerifyVenueByIdAsync(venueId);

        // 2. Create B2C user
        var b2CAuth = await Factory.AuthManager.CreateAndLoginUserAsync("b2c_checkin@example.com", "Password123!");
        Factory.ApiClient.SetAuth(b2CAuth);

        // 3. Check in as B2C user
        var checkInRequest = new CheckInRequest(null, null, newPinCode);
        var response = await Factory.ApiClient.PostAsync<CheckInRequest, ApiResult<CheckInResponse>>(
            ApiRoutes.Venue.QrCheckIn(venueId), checkInRequest);

        response.Success.ShouldBeTrue();
        response.Result.VenueId.ShouldBe(venueId);
    }

    [Fact]
    public async Task GivenVenue_WhenB2CUserChecksInWitWrongPinCode_ThenCheckInFailed()
    {
        // 1. Setup: B2B user creates verified venue
        var b2BAuth =
            await Factory.AuthManager.CreateAndLoginUserAsync("b2b_checkin@example.com", "Password123!", UserType.B2B);
        Factory.ApiClient.SetAuth(b2BAuth);

        var companyId = (await Factory.ApiClient.PostAsync<CreateCompanyRequest, ApiResult<CreateCompanyResponse>>(
            ApiRoutes.B2B.Company.Root, new CreateCompanyRequest("Checkin Co", "chk@example.com"))).Result.Id;

        var venueId = (await Factory.ApiClient.PostAsync<CreateVenueRequest, ApiResult<CreateVenueResponse>>(
                ApiRoutes.B2B.Company.Venues(companyId),
                new CreateVenueRequest(50, 50, "Checkin Venue", null, null, null, null, null, null, null)))
            .Result
            .Id;

        var newPinCode = "1234";
        var patchResponse = await Factory.ApiClient.PatchAsync<PatchVenueRequest, ApiResult.EmptyResult>(
            ApiRoutes.B2B.Venue.Single(venueId),
            new PatchVenueRequest(
                Latitude: null,
                Longitude: null,
                Address: null,
                WebsiteUrl: null,
                GoogleMapsUrl: null,
                InstagramUrl: null,
                PhoneNumber: null,
                WorkingHours: null,
                CoverImageUrl: null,
                PinCode: newPinCode,
                IsPinCheckInAllowed: true,
                QrCode: null,
                TagIds: null
            )
        );
        patchResponse.Success.ShouldBeTrue();

        await _testUtils.VerifyVenueByIdAsync(venueId);

        // 2. Create B2C user
        var b2CAuth = await Factory.AuthManager.CreateAndLoginUserAsync("b2c_checkin@example.com", "Password123!");
        Factory.ApiClient.SetAuth(b2CAuth);

        // 3. Check in as B2C user
        var checkInRequest = new CheckInRequest(null, null, newPinCode.Reverse().ToString());
        var response = await Factory.ApiClient.PostAsync<CheckInRequest, ApiResult<CheckInResponse>>(
            ApiRoutes.Venue.QrCheckIn(venueId), checkInRequest);

        response.Success.ShouldBeFalse();
        response.Result.VenueId.ShouldBe(venueId);
    }

    [Fact]
    public async Task GivenVenue_WhenB2CUserChecksInWithPinCodeAndPinCheckInDisabled_ThenCheckInFailed()
    {
        // 1. Setup: B2B user creates verified venue
        var b2BAuth =
            await Factory.AuthManager.CreateAndLoginUserAsync("b2b_checkin@example.com", "Password123!", UserType.B2B);
        Factory.ApiClient.SetAuth(b2BAuth);

        var companyId = (await Factory.ApiClient.PostAsync<CreateCompanyRequest, ApiResult<CreateCompanyResponse>>(
            ApiRoutes.B2B.Company.Root, new CreateCompanyRequest("Checkin Co", "chk@example.com"))).Result.Id;

        var venueId = (await Factory.ApiClient.PostAsync<CreateVenueRequest, ApiResult<CreateVenueResponse>>(
                ApiRoutes.B2B.Company.Venues(companyId),
                new CreateVenueRequest(50, 50, "Checkin Venue", null, null, null, null, null, null, null)))
            .Result
            .Id;

        var newPinCode = "1234";
        var patchResponse = await Factory.ApiClient.PatchAsync<PatchVenueRequest, ApiResult.EmptyResult>(
            ApiRoutes.B2B.Venue.Single(venueId),
            new PatchVenueRequest(
                Latitude: null,
                Longitude: null,
                Address: null,
                WebsiteUrl: null,
                GoogleMapsUrl: null,
                InstagramUrl: null,
                PhoneNumber: null,
                WorkingHours: null,
                CoverImageUrl: null,
                PinCode: newPinCode,
                IsPinCheckInAllowed: false,
                QrCode: null,
                TagIds: null
            )
        );
        patchResponse.Success.ShouldBeTrue();

        await _testUtils.VerifyVenueByIdAsync(venueId);

        // 2. Create B2C user
        var b2CAuth = await Factory.AuthManager.CreateAndLoginUserAsync("b2c_checkin@example.com", "Password123!");
        Factory.ApiClient.SetAuth(b2CAuth);

        // 3. Check in as B2C user
        var checkInRequest = new CheckInRequest(null, null, newPinCode);
        var response = await Factory.ApiClient.PostAsync<CheckInRequest, ApiResult<CheckInResponse>>(
            ApiRoutes.Venue.QrCheckIn(venueId), checkInRequest);

        response.Success.ShouldBeFalse();
        response.Result.VenueId.ShouldBe(venueId);
    }

    [Fact]
    public async Task GivenVenueWithPromotions_WhenB2CUserGetsDetail_ThenReturnsPromotions()
    {
        // 1. Create B2B user
        var b2BAuth = await Factory.AuthManager
            .CreateAndLoginUserAsync("b2b_promos@example.com", "Password123!", UserType.B2B);
        Factory.ApiClient.SetAuth(b2BAuth);

        // 2. Create company and venue
        var companyId = (await Factory.ApiClient.PostAsync<CreateCompanyRequest, ApiResult<CreateCompanyResponse>>(
            ApiRoutes.B2B.Company.Root, new CreateCompanyRequest("Promos Co", "promos@example.com"))).Result.Id;

        var venueResponse = await Factory.ApiClient.PostAsync<CreateVenueRequest, ApiResult<CreateVenueResponse>>(
            ApiRoutes.B2B.Company.Venues(companyId),
            new CreateVenueRequest(0, 0, "Promos Venue", null, null, null, null, null, null, null));
        var venueId = venueResponse.Result.Id;

        // 3. Add promotions
        var promo1 = new CreatePromotionRequest(PromoType.General, "Test Promotion 1");
        var promo2 = new CreatePromotionRequest(PromoType.HappyHours, "Test Promotion 2");

        var p1Response = await Factory.ApiClient.PostAsync<CreatePromotionRequest, ApiResult<CreatePromotionResponse>>(
            ApiRoutes.B2B.Venue.Promotions(venueId), promo1);
        p1Response.Success.ShouldBeTrue();

        var p2Response = await Factory.ApiClient.PostAsync<CreatePromotionRequest, ApiResult<CreatePromotionResponse>>(
            ApiRoutes.B2B.Venue.Promotions(venueId), promo2);
        p2Response.Success.ShouldBeTrue();

        // 4. Create B2C user
        var b2CAuth = await Factory.AuthManager.CreateAndLoginUserAsync("b2c_promos@example.com", "Password123!");
        Factory.ApiClient.SetAuth(b2CAuth);

        // 5. Get venue detail and check promotions
        var response = await Factory.ApiClient.GetAsync<ApiResult<GetVenueResponse>>(
            ApiRoutes.Venue.Single(venueId));

        response.Success.ShouldBeTrue();
        var promos = response.Result.Venue.Promos;
        promos.Length.ShouldBe(2);
        promos.ShouldContain(p => p.Type == promo1.Type && p.Body == promo1.Body);
        promos.ShouldContain(p => p.Type == promo2.Type && p.Body == promo2.Body);
    }

    [Fact]
    public async Task GivenUserWithMaxLoyaltyLevel_WhenDisablesLoyaltyLevel_ThenUserGetsNextAvailableDiscount()
    {
        // 1. Create B2B user
        var b2BAuth = await Factory.AuthManager
            .CreateAndLoginUserAsync("b2b_discounts@example.com", "Password123!", UserType.B2B);
        Factory.ApiClient.SetAuth(b2BAuth);

        // 2. Create all required entities
        var levels = new[]
        {
            new LoyaltyLevelDto(0, 1, true),
            new LoyaltyLevelDto(1, 2, true),
            new LoyaltyLevelDto(2, 3, true),
            new LoyaltyLevelDto(3, 4, true)
        };

        var today = Factory.FakeTimeProvider.GetUtcNow().DayOfWeek;
        var discounts = new[]
        {
            new CreateDiscountRequest(0, 5, [today]),
            new CreateDiscountRequest(1, 10, [today]),
            new CreateDiscountRequest(2, 15, [today]),
            new CreateDiscountRequest(3, 20, [today]),
        };

        var (companyId, venueId) = await _testUtils.SetupLoyaltyLevelsAndCreateDiscounts(levels, discounts);
        var b2CAuth = await _testUtils.CreateB2CUserAndReachLoyaltyLevel(
            "b2c@example.com", "Password123!",
            levels, 3, venueId);

        // 3. Disable 3rd loyalty level
        Factory.ApiClient.SetAuth(b2BAuth);
        await Factory.ApiClient.PutAsync<UpdateLoyaltyLevelRequest, ApiResult.EmptyResult>(
            ApiRoutes.B2B.Company.LoyaltyLevels(companyId),
            new UpdateLoyaltyLevelRequest(new LoyaltyLevelDto(3, 4, false)));

        // 4. Ensure Level 2 discount is applied
        Factory.ApiClient.SetAuth(b2CAuth);
        var response = await Factory.ApiClient.GetAsync<ApiResult<GetVenueResponse>>(
            ApiRoutes.Venue.Single(venueId));
        response.Success.ShouldBeTrue();
        response.Result.LoyaltyLevel.Discount.ShouldBe(15);
    }

    [Fact]
    public async Task GivenUserWithMaxLoyaltyLevel_WhenGetLoyaltyLevelFromVenue_ThenUpperBoundIsNull()
    {
        // 1. Create B2B user
        var b2BAuth = await Factory.AuthManager
            .CreateAndLoginUserAsync("b2b_discounts@example.com", "Password123!", UserType.B2B);
        Factory.ApiClient.SetAuth(b2BAuth);

        // 2. Create all required entities
        var levels = new[]
        {
            new LoyaltyLevelDto(0, 1, true),
            new LoyaltyLevelDto(1, 2, true),
            new LoyaltyLevelDto(2, 3, false),
            new LoyaltyLevelDto(3, 4, false)
        };

        var targetLevel = 1;
        var (_, venueId) = await _testUtils.SetupLoyaltyLevelsAndCreateDiscounts(levels, []);
        var b2CAuth = await _testUtils.CreateB2CUserAndReachLoyaltyLevel(
            "b2c@example.com", "Password123!",
            levels, targetLevel, venueId);

        // 3. Ensure Level 2 discount is applied
        Factory.ApiClient.SetAuth(b2CAuth);
        var response = await Factory.ApiClient.GetAsync<ApiResult<GetVenueResponse>>(
            ApiRoutes.Venue.Single(venueId));
        response.Success.ShouldBeTrue();
        response.Result.LoyaltyLevel.VisitsUpperBound.ShouldBeNull();
        response.Result.LoyaltyLevel.UserLoyaltyLevel.ShouldBe(targetLevel);
        response.Result.LoyaltyLevel.Discount.ShouldBe(0);
    }

    [Fact]
    public async Task GivenUserWithoutLoyaltyLevel_WhenGetLoyaltyLevelFromVenue_ThenItZeroWithNullUpperBound()
    {
        // 1. Create B2B user
        var b2BAuth = await Factory.AuthManager
            .CreateAndLoginUserAsync("b2b_discounts@example.com", "Password123!", UserType.B2B);
        Factory.ApiClient.SetAuth(b2BAuth);

        // 2. Create all required entities
        var levels = new[]
        {
            new LoyaltyLevelDto(0, 1, false),
            new LoyaltyLevelDto(1, 2, false),
            new LoyaltyLevelDto(2, 3, false),
            new LoyaltyLevelDto(3, 4, false)
        };

        var targetLevel = 2;
        var (_, venueId) = await _testUtils.SetupLoyaltyLevelsAndCreateDiscounts(levels, []);
        var b2CAuth = await _testUtils.CreateB2CUserAndReachLoyaltyLevel(
            "b2c@example.com", "Password123!",
            levels, targetLevel, venueId);

        // 3. Ensure Level 2 discount is applied
        Factory.ApiClient.SetAuth(b2CAuth);
        var response = await Factory.ApiClient.GetAsync<ApiResult<GetVenueResponse>>(
            ApiRoutes.Venue.Single(venueId));
        response.Success.ShouldBeTrue();
        response.Result.LoyaltyLevel.VisitsUpperBound.ShouldBeNull();
        response.Result.LoyaltyLevel.UserLoyaltyLevel.ShouldBe(0);
        response.Result.LoyaltyLevel.Discount.ShouldBe(0);
    }

    [Fact]
    public async Task GivenVenue_WhenB2CUserGetsCheckInSettings_ThenReturnsValidData()
    {
        // 1. Create B2B user
        var b2BAuth = await Factory.AuthManager
            .CreateAndLoginUserAsync("b2b_detail@example.com", "Password123!", UserType.B2B);
        Factory.ApiClient.SetAuth(b2BAuth);

        // 2. Create company and venue
        var companyId = (await Factory.ApiClient.PostAsync<CreateCompanyRequest, ApiResult<CreateCompanyResponse>>(
            ApiRoutes.B2B.Company.Root, new CreateCompanyRequest("Detail Co", "det@example.com"))).Result.Id;

        var venueRequest = new CreateVenueRequest(45, 13, "Detail Venue", "https://site.com", "https://google.com/mo",
            "https://instagram.com/some-venue", "123", "9-5", TestConfig.S3.PublicUrl + "/cover.jpg", null);
        var venueResponse = await Factory.ApiClient.PostAsync<CreateVenueRequest, ApiResult<CreateVenueResponse>>(
            ApiRoutes.B2B.Company.Venues(companyId), venueRequest);
        venueResponse.Success.ShouldBeTrue();
        var venueId = venueResponse.Result.Id;

        // 3. Create B2C user
        var b2CAuth = await Factory.AuthManager.CreateAndLoginUserAsync("b2c_detail@example.com", "Password123!");
        Factory.ApiClient.SetAuth(b2CAuth);

        // 4. Get venues and check data
        var disabledResponse = await Factory.ApiClient.GetAsync<ApiResult<GetCheckInSettingsResponse>>(
            ApiRoutes.Venue.CheckInSetting(venueId));
        disabledResponse.Success.ShouldBeTrue();
        disabledResponse.Result.IsPinCheckInAllowed.ShouldBeFalse();

        // 4. Update IsPinCheckInAllowed flag
        Factory.ApiClient.SetAuth(b2BAuth);
        var patchResponse = await Factory.ApiClient.PatchAsync<PatchVenueRequest, ApiResult.EmptyResult>(
            ApiRoutes.B2B.Venue.Single(venueId),
            new PatchVenueRequest(
                Latitude: null,
                Longitude: null,
                Address: null,
                WebsiteUrl: null,
                GoogleMapsUrl: null,
                InstagramUrl: null,
                PhoneNumber: null,
                WorkingHours: null,
                CoverImageUrl: null,
                PinCode: null,
                IsPinCheckInAllowed: true,
                QrCode: null,
                TagIds: null
            )
        );
        patchResponse.Success.ShouldBeTrue();

        // 4. Get venues and check data
        Factory.ApiClient.SetAuth(b2CAuth);
        var enabledResponse = await Factory.ApiClient.GetAsync<ApiResult<GetCheckInSettingsResponse>>(
            ApiRoutes.Venue.CheckInSetting(venueId));
        enabledResponse.Success.ShouldBeTrue();
        enabledResponse.Result.IsPinCheckInAllowed.ShouldBeTrue();
    }

    [Fact]
    public async Task GivenVenues_WhenB2CUserGetsMap_ThenReturnsVenuesInBounds()
    {
        // 1. Create B2B user
        var b2BAuth = await Factory.AuthManager
            .CreateAndLoginUserAsync("b2b_map@example.com", "Password123!", UserType.B2B);
        Factory.ApiClient.SetAuth(b2BAuth);

        var companyId = (await Factory.ApiClient.PostAsync<CreateCompanyRequest, ApiResult<CreateCompanyResponse>>(
            ApiRoutes.B2B.Company.Root, new CreateCompanyRequest("Map Co", "map@example.com"))).Result.Id;

        // 2. Create venues in different locations
        // Bounds for search: Lat [44.7, 44.9], Lng [20.4, 20.6]
        await _testUtils.CreateVerifiedVenueAsync(companyId, 44.8, 20.5, "Inside Venue");
        await _testUtils.CreateVerifiedVenueAsync(companyId, 45.0, 21.0, "Outside Venue");

        // 3. Create B2C user
        var b2CAuth = await Factory.AuthManager.CreateAndLoginUserAsync("b2c_map@example.com", "Password123!");
        Factory.ApiClient.SetAuth(b2CAuth);

        // 4. Get venues for map and check results
        var mapRequest = new GetVenuesMapRequest(
            MinLat: 44.7,
            MaxLat: 44.9,
            MinLng: 20.4,
            MaxLng: 20.6);

        var response = await Factory.ApiClient.GetAsync<GetVenuesMapRequest, ApiResult<VenueMapItemDto[]>>(
            ApiRoutes.Venue.Map, mapRequest);

        response.Success.ShouldBeTrue();
        response.Result.Length.ShouldBe(1);
        response.Result[0].Address.ShouldBe("Inside Venue");
        response.Result.ShouldNotContain(v => v.Address == "Outside Venue");
    }
}