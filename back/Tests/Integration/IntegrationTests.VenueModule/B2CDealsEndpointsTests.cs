using IntegrationTests.Shared;
using SecurityModule.API.Dto;
using SecurityModule.Domain.ValueObjects;
using SharedCore.Api.Dto;
using Shouldly;
using VenueModule.API.B2B.Deals.Dto;
using VenueModule.API.B2C.Dto;

namespace IntegrationTests.VenueModule;

[Collection("IntegrationTests")]
public class B2CDealsEndpointsTests : IntegrationTestBase, IAsyncLifetime
{
    private readonly TestUtils _testUtils;

    public B2CDealsEndpointsTests(IntegrationWebApplicationFactory factory) : base(factory)
    {
        _testUtils = new TestUtils(factory);
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public Task DisposeAsync() => ContainerManager.ResetDatabaseAsync();

    [Fact]
    public async Task GivenMultipleVenuesWithDiscounts_WhenB2CUserListsDiscounts_ThenReturnsSortedCorrectly()
    {
        // 1. Create B2B user
        var b2BAuth =
            await Factory.AuthManager.CreateAndLoginUserAsync("b2b_discounts_sort@example.com", "Password123!",
                UserType.B2B);
        Factory.ApiClient.SetAuth(b2BAuth);

        // 2. Setup companies and venues

        // Venue A: Company "Company A", Dist 10 (approx), Percent 10
        var companyAId = await _testUtils.CreateCompanyAsync("Company A", "a@example.com");
        await Factory.ApiClient.PutAsync<UpdateLoyaltyLevelsRequest, ApiResult.EmptyResult>(
            ApiRoutes.B2B.Company.LoyaltyLevels(companyAId),
            new UpdateLoyaltyLevelsRequest(new[] { new LoyaltyLevelDto(0, 1, true) }));
        var venueAId = await _testUtils.CreateVerifiedVenueAsync(companyAId, 0.1, 0.1, "Address A"); // ~15km
        await _testUtils.AddDiscountAsync(venueAId, companyAId, 10);

        // Venue B: Company "Company B", Dist 5 (approx), Percent 10
        var companyBId = await _testUtils.CreateCompanyAsync("Company B", "b@example.com");
        await Factory.ApiClient.PutAsync<UpdateLoyaltyLevelsRequest, ApiResult.EmptyResult>(
            ApiRoutes.B2B.Company.LoyaltyLevels(companyBId),
            new UpdateLoyaltyLevelsRequest(new[] { new LoyaltyLevelDto(0, 1, true) }));
        var venueBId = await _testUtils.CreateVerifiedVenueAsync(companyBId, 0.05, 0.05, "Address B"); // ~7.8km
        await _testUtils.AddDiscountAsync(venueBId, companyBId, 10);

        // Venue C: Company "Company C", Dist 5 (approx), Percent 20
        var companyCId = await _testUtils.CreateCompanyAsync("Company C", "c@example.com");
        await Factory.ApiClient.PutAsync<UpdateLoyaltyLevelsRequest, ApiResult.EmptyResult>(
            ApiRoutes.B2B.Company.LoyaltyLevels(companyCId),
            new UpdateLoyaltyLevelsRequest(new[] { new LoyaltyLevelDto(0, 1, true) }));
        var venueCId = await _testUtils.CreateVerifiedVenueAsync(companyCId, 0.051, 0.051, "Address C"); // ~8km
        await _testUtils.AddDiscountAsync(venueCId, companyCId, 20);

        // Venue D: Company "Company D", Dist 5 (approx), Percent 10
        var companyDId = await _testUtils.CreateCompanyAsync("Company D", "d@example.com");
        await Factory.ApiClient.PutAsync<UpdateLoyaltyLevelsRequest, ApiResult.EmptyResult>(
            ApiRoutes.B2B.Company.LoyaltyLevels(companyDId),
            new UpdateLoyaltyLevelsRequest(new[] { new LoyaltyLevelDto(0, 1, true) }));
        var venueDId = await _testUtils.CreateVerifiedVenueAsync(companyDId, 0.05, 0.05, "Address D"); // ~7.8km
        await _testUtils.AddDiscountAsync(venueDId, companyDId, 10);

        // Venue E: Company "Company D", Custom
        var companyEId = await _testUtils.CreateCompanyAsync("Company E", "e@example.com");
        await Factory.ApiClient.PutAsync<UpdateLoyaltyLevelsRequest, ApiResult.EmptyResult>(
            ApiRoutes.B2B.Company.LoyaltyLevels(companyEId),
            new UpdateLoyaltyLevelsRequest(new[] { new LoyaltyLevelDto(0, 1, true) }));
        var venueEId = await _testUtils.CreateVerifiedVenueAsync(companyEId, 0.05, 0.05, "Address E"); // ~7.8km
        await _testUtils.AddDiscountAsync(venueEId, companyEId, null);

        // 3. Create B2C user at (0,0)
        var b2CAuth =
            await Factory.AuthManager.CreateAndLoginUserAsync("b2c_discounts_sort@example.com", "Password123!");
        Factory.ApiClient.SetAuth(b2CAuth);

        // 4. Get discounts list
        var response = await Factory.ApiClient.GetAsync<ApiResult<PaginatedResponse<AvailableDiscountDto>>>(
            $"{ApiRoutes.Deals.Discounts}?Latitude=0&Longitude=0&Offset=0&Limit=10");

        response.Success.ShouldBeTrue();
        var discounts = response.Result.Data.ToArray();

        // Expected Order:
        // 1. Venue C (20%)
        // 2. Venue B (10%, dist 7.8km, "Company B")
        // 3. Venue D (10%, dist 7.8km, "Company D")
        // 4. Venue A (10%, dist 15km)
        // 4. Venue E (Custom)

        discounts.Length.ShouldBe(5);
        discounts[0].VenueId.ShouldBe(venueCId);
        discounts[0].Percent.ShouldBe(20);

        discounts[1].VenueId.ShouldBe(venueBId);
        discounts[1].Percent.ShouldBe(10);
        discounts[1].CompanyName.ShouldBe("Company B");

        discounts[2].VenueId.ShouldBe(venueDId);
        discounts[2].Percent.ShouldBe(10);
        discounts[2].CompanyName.ShouldBe("Company D");

        discounts[3].VenueId.ShouldBe(venueAId);
        discounts[3].Percent.ShouldBe(10);
        discounts[3].CompanyName.ShouldBe("Company A");

        discounts[4].VenueId.ShouldBe(venueEId);
        discounts[4].Percent.ShouldBeNull();
        discounts[4].CompanyName.ShouldBe("Company E");
    }

    [Fact]
    public async Task GivenMultipleVenuesWithPromos_WhenB2CUserListsPromos_ThenReturnsSortedCorrectly()
    {
        // 1. Create B2B user
        var b2BAuth =
            await Factory.AuthManager.CreateAndLoginUserAsync("b2b_promos_sort@example.com", "Password123!",
                UserType.B2B);
        Factory.ApiClient.SetAuth(b2BAuth);

        // 2. Setup companies and venues

        // Venue A: Company "Company A", Dist 10, Promo Count 1
        var companyAId = await _testUtils.CreateCompanyAsync("Company A", "a_p@example.com");
        var venueAId = await _testUtils.CreateVerifiedVenueAsync(companyAId, 0.1, 0.1, "Address A");
        await _testUtils.AddPromotionAsync(venueAId, "Promo A1");

        // Venue B: Company "Company B", Dist 5, Promo Count 1
        var companyBId = await _testUtils.CreateCompanyAsync("Company B", "b_p@example.com");
        var venueBId = await _testUtils.CreateVerifiedVenueAsync(companyBId, 0.05, 0.05, "Address B");
        await _testUtils.AddPromotionAsync(venueBId, "Promo B1");

        // Venue C: Company "Company C", Dist 5, Promo Count 2
        var companyCId = await _testUtils.CreateCompanyAsync("Company C", "c_p@example.com");
        var venueCId = await _testUtils.CreateVerifiedVenueAsync(companyCId, 0.051, 0.051, "Address C");
        await _testUtils.AddPromotionAsync(venueCId, "Promo C1");
        await _testUtils.AddPromotionAsync(venueCId, "Promo C2");

        // Venue D: Company "Company D", Dist 5, Promo Count 1
        var companyDId = await _testUtils.CreateCompanyAsync("Company D", "d_p@example.com");
        var venueDId = await _testUtils.CreateVerifiedVenueAsync(companyDId, 0.05, 0.05, "Address D");
        await _testUtils.AddPromotionAsync(venueDId, "Promo D1");

        // 3. Create B2C user at (0,0)
        var b2CAuth = await Factory.AuthManager.CreateAndLoginUserAsync("b2c_promos_sort@example.com", "Password123!");
        Factory.ApiClient.SetAuth(b2CAuth);

        // 4. Get promotions list
        var response = await Factory.ApiClient.GetAsync<ApiResult<PaginatedResponse<AvailablePromotionDto>>>(
            $"{ApiRoutes.Deals.Promotions}?Latitude=0&Longitude=0&Offset=0&Limit=10");

        response.Success.ShouldBeTrue();
        var promos = response.Result.Data.ToArray();

        // Expected Order:
        // 1. Venue C (2 promos)
        // 2. Venue B (1 promo, dist 7.8km, "Company B")
        // 3. Venue D (1 promo, dist 7.8km, "Company D")
        // 4. Venue A (1 promo, dist 15km)

        promos.Length.ShouldBe(4);
        promos[0].VenueId.ShouldBe(venueCId.ToString());
        promos[0].Promos.Length.ShouldBe(2);

        promos[1].VenueId.ShouldBe(venueBId.ToString());
        promos[1].Promos.Length.ShouldBe(1);
        promos[1].CompanyName.ShouldBe("Company B");

        promos[2].VenueId.ShouldBe(venueDId.ToString());
        promos[2].Promos.Length.ShouldBe(1);
        promos[2].CompanyName.ShouldBe("Company D");

        promos[3].VenueId.ShouldBe(venueAId.ToString());
        promos[3].Promos.Length.ShouldBe(1);
        promos[3].CompanyName.ShouldBe("Company A");
    }

    [Fact]
    public async Task GivenUserWithLevel2_WhenVenueHasDiscountsForLevels012_ThenLevel2DiscountApplied()
    {
        var levels = new[]
        {
            new LoyaltyLevelDto(0, 1, true),
            new LoyaltyLevelDto(1, 2, true),
            new LoyaltyLevelDto(2, 3, true),
            new LoyaltyLevelDto(3, 100, false)
        };

        var today = Factory.FakeTimeProvider.GetUtcNow().DayOfWeek;
        var discounts = new[]
        {
            new CreateDiscountRequest(0, 5, [today]),
            new CreateDiscountRequest(1, 10, [today]),
            new CreateDiscountRequest(2, 15, [today])
        };

        await CreateB2BUser();
        var (_, venueId) = await _testUtils.SetupLoyaltyLevelsAndCreateDiscounts(levels, discounts);
        await _testUtils.CreateB2CUserAndReachLoyaltyLevel(
            "b2c_tiered_b2c@example.com", "Password123!",
            levels, 2, venueId);

        // Ensure Level 2 discount is applied
        var response = await Factory.ApiClient.GetAsync<ApiResult<PaginatedResponse<AvailableDiscountDto>>>(
            $"{ApiRoutes.Deals.Discounts}?Latitude=0&Longitude=0&Offset=0&Limit=10");
        response.Success.ShouldBeTrue();
        response.Result.Data.First().Percent.ShouldBe(15);
    }

    [Fact]
    public async Task GivenUserWithMaxLoyaltyLevel_WhenDisablesLoyaltyLevel_ThenUserGetsNextAvailableDiscount()
    {
        // 1. Create all required entities
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

        var b2BAuth = await CreateB2BUser();
        var (companyId, venueId) = await _testUtils.SetupLoyaltyLevelsAndCreateDiscounts(levels, discounts);
        var b2CAuth = await _testUtils.CreateB2CUserAndReachLoyaltyLevel(
            "b2c_tiered_b2c@example.com", "Password123!",
            levels, 3, venueId);

        // 2. Disable 3rd loyalty level
        Factory.ApiClient.SetAuth(b2BAuth);
        await Factory.ApiClient.PutAsync<UpdateLoyaltyLevelsRequest, ApiResult.EmptyResult>(
            ApiRoutes.B2B.Company.LoyaltyLevels(companyId),
            new UpdateLoyaltyLevelsRequest(new[] { new LoyaltyLevelDto(3, 4, false) }));

        // 3. Ensure Level 2 discount is applied
        Factory.ApiClient.SetAuth(b2CAuth);
        var response = await Factory.ApiClient.GetAsync<ApiResult<PaginatedResponse<AvailableDiscountDto>>>(
            $"{ApiRoutes.Deals.Discounts}?Latitude=0&Longitude=0&Offset=0&Limit=10");
        response.Success.ShouldBeTrue();
        response.Result.Data.First().Percent.ShouldBe(15);
    }

    private async Task<AuthResponse> CreateB2BUser()
    {
        var b2BAuth = await Factory.AuthManager
            .CreateAndLoginUserAsync("b2b_tiered_b2c@example.com", "Password123!", UserType.B2B);
        Factory.ApiClient.SetAuth(b2BAuth);
        return b2BAuth;
    }
}