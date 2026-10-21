using System.Net;
using System.Net.Http.Json;
using IntegrationTests.Shared;
using SecurityModule.Domain.ValueObjects;
using Shouldly;
using VenueModule.API.B2B.Companies.Dto;
using VenueModule.API.B2B.Deals.Dto;
using VenueModule.API.B2B.Venues.Dto;
using VenueModule.Domain.ValueObjects;

namespace IntegrationTests.VenueModule;

[Collection("IntegrationTests")]
public class B2BDealsEndpointsTests : IntegrationTestBase, IAsyncLifetime
{
    public B2BDealsEndpointsTests(IntegrationWebApplicationFactory factory) : base(factory)
    {
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public Task DisposeAsync() => ContainerManager.ResetDatabaseAsync();

    [Fact]
    public async Task GivenAuthenticatedB2BUser_WhenUpdatesLoyaltyLevels_ThenLoyaltyLevelsUpdated()
    {
        // 1. Sign in as B2B user and create company
        var authResponse = await Factory.AuthManager
            .CreateAndLoginUserAsync("b2b_loyalty@example.com", "Password123!", UserType.B2B);
        Factory.ApiClient.SetAuth(authResponse);

        var createCompanyResponse =
            await Factory.ApiClient.PostAsync<CreateCompanyRequest, CreateCompanyResponse>(
                ApiRoutes.B2B.Company.Root, new CreateCompanyRequest("Loyalty Co", "loyalty@test.com"));
        var companyId = createCompanyResponse.Id;

        // 2. Get initial loyalty levels (should have 4 default levels)
        var getLevelsResponse = await Factory.ApiClient.GetAsync<LoyaltyLevelDto[]>(
            ApiRoutes.B2B.Company.LoyaltyLevels(companyId));
        getLevelsResponse.Length.ShouldBe(4);
        var firstLevel = getLevelsResponse[0];

        // 3. Update one loyalty level
        var updateRequest = new UpdateLoyaltyLevelsRequest(
            new[] { new LoyaltyLevelDto(firstLevel.Id, 15, true) });

        await Factory.ApiClient.PutAsync<UpdateLoyaltyLevelsRequest>(
            ApiRoutes.B2B.Company.LoyaltyLevels(companyId), updateRequest);

        // 4. Verify update
        var getUpdatedResponse = await Factory.ApiClient.GetAsync<GetCompanyResponse>(
            ApiRoutes.B2B.Company.Single(companyId));
        getUpdatedResponse.Company.LoyaltyLevels.ShouldContain(l =>
            l.Id == firstLevel.Id && l.VisitsUpperBound == 15 && l.IsActive);
    }

    [Fact]
    public async Task GivenAuthenticatedB2BUser_WhenCreatesPromotion_ThenPromotionCreated()
    {
        // 1. Sign in as B2B user and create company/venue
        var authResponse = await Factory.AuthManager
            .CreateAndLoginUserAsync("b2b_promo@example.com", "Password123!", UserType.B2B);
        Factory.ApiClient.SetAuth(authResponse);

        var createCompanyResponse =
            await Factory.ApiClient.PostAsync<CreateCompanyRequest, CreateCompanyResponse>(
                ApiRoutes.B2B.Company.Root, new CreateCompanyRequest("Promo Co", "promo@test.com"));
        var companyId = createCompanyResponse.Id;

        var createVenueResponse = await Factory.ApiClient.PostAsync<CreateVenueRequest, CreateVenueResponse>(
            ApiRoutes.B2B.Company.Venues(companyId),
            new CreateVenueRequest(0, 0, "Promo Venue", null, null, null, null, null, null, []));
        var venueId = createVenueResponse.Id;

        // 2. Create promotion
        var createPromoRequest = new CreatePromotionRequest(PromoType.HappyHours, "Happy Friday!");
        var createPromoResponse =
            await Factory.ApiClient.PostAsync<CreatePromotionRequest, CreatePromotionResponse>(
                ApiRoutes.B2B.Venue.Promotions(venueId), createPromoRequest);
        var promoId = createPromoResponse.Id;

        // 3. Verify promotion in venue detail
        var getVenueResponse = await Factory.ApiClient.GetAsync<GetVenueResponse>(
            ApiRoutes.B2B.Venue.Single(venueId));
        getVenueResponse.Venue.Promos.ShouldContain(p =>
            p.Id == promoId.ToString() && p.Type == createPromoRequest.Type && p.Body == createPromoRequest.Body);
    }

    [Fact]
    public async Task GivenExistingPromotion_WhenUpdatesPromotion_ThenPromotionUpdated()
    {
        // 1. Sign in and create promotion
        var authResponse = await Factory.AuthManager
            .CreateAndLoginUserAsync("b2b_promo_update@example.com", "Password123!", UserType.B2B);
        Factory.ApiClient.SetAuth(authResponse);

        var createCoResponse =
            await Factory.ApiClient.PostAsync<CreateCompanyRequest, CreateCompanyResponse>(
                ApiRoutes.B2B.Company.Root, new CreateCompanyRequest("Promo Update Co", "pu@test.com"));
        var createVenueResponse = await Factory.ApiClient.PostAsync<CreateVenueRequest, CreateVenueResponse>(
            ApiRoutes.B2B.Company.Venues(createCoResponse.Id),
            new CreateVenueRequest(0, 0, "PU Venue", null, null, null, null, null, null, []));
        var venueId = createVenueResponse.Id;

        var createPromoResponse =
            await Factory.ApiClient.PostAsync<CreatePromotionRequest, CreatePromotionResponse>(
                ApiRoutes.B2B.Venue.Promotions(venueId), new CreatePromotionRequest(PromoType.General, "Old Promo"));
        var promoId = createPromoResponse.Id;

        // 2. Update promotion
        var updateRequest = new UpdatePromotionRequest(PromoType.Event, "New Promo Name");
        await Factory.ApiClient.PutAsync<UpdatePromotionRequest>(
            ApiRoutes.B2B.Venue.Promotion(venueId, promoId), updateRequest);

        // 3. Verify update
        var getVenueResponse = await Factory.ApiClient.GetAsync<GetVenueResponse>(
            ApiRoutes.B2B.Venue.Single(venueId));
        getVenueResponse.Venue.Promos.ShouldContain(p =>
            p.Id == promoId.ToString() && p.Type == updateRequest.Type && p.Body == updateRequest.Body);
    }

    [Fact]
    public async Task GivenExistingPromotion_WhenDeletesPromotion_ThenPromotionDeleted()
    {
        // 1. Sign in and create promotion
        var authResponse = await Factory.AuthManager
            .CreateAndLoginUserAsync("b2b_promo_delete@example.com", "Password123!", UserType.B2B);
        Factory.ApiClient.SetAuth(authResponse);

        var createCoResponse =
            await Factory.ApiClient.PostAsync<CreateCompanyRequest, CreateCompanyResponse>(
                ApiRoutes.B2B.Company.Root, new CreateCompanyRequest("Promo Del Co", "pd@test.com"));
        var venueId = (await Factory.ApiClient.PostAsync<CreateVenueRequest, CreateVenueResponse>(
            ApiRoutes.B2B.Company.Venues(createCoResponse.Id),
            new CreateVenueRequest(0, 0, "PD Venue", null, null, null, null, null, null, []))).Id;

        var promoId = (await Factory.ApiClient.PostAsync<CreatePromotionRequest, CreatePromotionResponse>(
                ApiRoutes.B2B.Venue.Promotions(venueId), new CreatePromotionRequest(PromoType.General, "To Delete")))

            .Id;

        // 2. Delete promotion
        await Factory.ApiClient.DeleteAsync(
            ApiRoutes.B2B.Venue.Promotion(venueId, promoId));

        // 3. Verify deleted
        var getVenueResponse = await Factory.ApiClient.GetAsync<GetVenueResponse>(
            ApiRoutes.B2B.Venue.Single(venueId));
        getVenueResponse.Venue.Promos.ShouldNotContain(p => p.Id == promoId.ToString());
    }

    [Fact]
    public async Task GivenAuthenticatedB2BUser_WhenCreatesDiscount_ThenDiscountCreated()
    {
        // 1. Sign in and create company/venue
        var authResponse = await Factory.AuthManager
            .CreateAndLoginUserAsync("b2b_discount@example.com", "Password123!", UserType.B2B);
        Factory.ApiClient.SetAuth(authResponse);

        var createCompanyResponse =
            await Factory.ApiClient.PostAsync<CreateCompanyRequest, CreateCompanyResponse>(
                ApiRoutes.B2B.Company.Root, new CreateCompanyRequest("Disc Co", "disc@test.com"));
        var companyId = createCompanyResponse.Id;
        var venueId = (await Factory.ApiClient.PostAsync<CreateVenueRequest, CreateVenueResponse>(
            ApiRoutes.B2B.Company.Venues(companyId),
            new CreateVenueRequest(0, 0, "Disc Venue", null, null, null, null, null, null, []))).Id;

        var loyaltyLevels =
            (await Factory.ApiClient.GetAsync<LoyaltyLevelDto[]>(
                ApiRoutes.B2B.Company.LoyaltyLevels(companyId)));
        var levelId = loyaltyLevels[0].Id;

        // 2. Create discount A
        var createRequestA = new CreateDiscountRequest(levelId, 10, [DayOfWeek.Monday, DayOfWeek.Friday]);
        var createResponseA =
            await Factory.ApiClient.PostAsync<CreateDiscountRequest, CreateDiscountResponse>(
                ApiRoutes.B2B.Venue.Discounts(venueId), createRequestA);

        // 3. Create discount B
        var createRequestB = new CreateDiscountRequest(levelId, 15, [DayOfWeek.Tuesday, DayOfWeek.Wednesday]);
        var createResponseB =
            await Factory.ApiClient.PostAsync<CreateDiscountRequest, CreateDiscountResponse>(
                ApiRoutes.B2B.Venue.Discounts(venueId), createRequestB);

        // 3. Create discount C
        var createRequestC = new CreateDiscountRequest(levelId, null, [DayOfWeek.Saturday]);
        var createResponseC =
            await Factory.ApiClient.PostAsync<CreateDiscountRequest, CreateDiscountResponse>(
                ApiRoutes.B2B.Venue.Discounts(venueId), createRequestC);

        // 4. Verify discounts in list
        var getVenueResponse = await Factory.ApiClient.GetAsync<GetVenueResponse>(
            ApiRoutes.B2B.Venue.Single(venueId));
        getVenueResponse.Venue.Discounts.Length.ShouldBe(3);
        var discountA = getVenueResponse.Venue.Discounts[0];
        discountA.ShouldNotBeNull();
        discountA.Percent.ShouldBe(createRequestB.Percent);
        discountA.LoyaltyLevelId.ShouldBe(createRequestB.LoyaltyLevelId);
        discountA.Days.ShouldBe(createRequestB.Days);

        var discountB = getVenueResponse.Venue.Discounts[1];
        discountB.ShouldNotBeNull();
        discountB.Percent.ShouldBe(createRequestA.Percent);
        discountB.LoyaltyLevelId.ShouldBe(createRequestA.LoyaltyLevelId);
        discountB.Days.ShouldBe(createRequestA.Days);

        var discountC = getVenueResponse.Venue.Discounts[2];
        discountC.ShouldNotBeNull();
        discountC.Percent.ShouldBe(createRequestC.Percent);
        discountC.LoyaltyLevelId.ShouldBe(createRequestC.LoyaltyLevelId);
        discountC.Days.ShouldBe(createRequestC.Days);
    }

    [Fact]
    public async Task GivenExistingDiscount_WhenUpdatesDiscount_ThenDiscountUpdated()
    {
        // 1. Sign in and create discount
        var authResponse = await Factory.AuthManager
            .CreateAndLoginUserAsync("b2b_discount_update@example.com", "Password123!", UserType.B2B);
        Factory.ApiClient.SetAuth(authResponse);

        var companyId = (await Factory.ApiClient.PostAsync<CreateCompanyRequest, CreateCompanyResponse>(
            ApiRoutes.B2B.Company.Root, new CreateCompanyRequest("Disc Up Co", "du@test.com"))).Id;
        var venueId = (await Factory.ApiClient.PostAsync<CreateVenueRequest, CreateVenueResponse>(
            ApiRoutes.B2B.Company.Venues(companyId),
            new CreateVenueRequest(0, 0, "DU Venue", null, null, null, null, null, null, []))).Id;
        var levelId = (await Factory.ApiClient.GetAsync<LoyaltyLevelDto[]>(
            ApiRoutes.B2B.Company.LoyaltyLevels(companyId)))[0].Id;

        var discountId = (await Factory.ApiClient.PostAsync<CreateDiscountRequest, CreateDiscountResponse>(
                ApiRoutes.B2B.Venue.Discounts(venueId), new CreateDiscountRequest(levelId, 5, [DayOfWeek.Monday])))
            .Id;

        // 2. Update discount
        var updateRequest = new UpdateDiscountRequest(levelId, 15, [DayOfWeek.Saturday, DayOfWeek.Sunday]);
        await Factory.ApiClient.PutAsync<UpdateDiscountRequest>(
            ApiRoutes.B2B.Venue.Discount(venueId, discountId), updateRequest);

        // 3. Verify update
        var getVenueResponse = await Factory.ApiClient.GetAsync<GetVenueResponse>(
            ApiRoutes.B2B.Venue.Single(venueId));
        getVenueResponse.Venue.Discounts.Length.ShouldBe(1);
        var discount = getVenueResponse.Venue.Discounts.First();
        discount.Id.ShouldBe(discountId.ToString());
        discount.Percent.ShouldBe(updateRequest.Percent!.Value);
        discount.Days.ShouldBe(updateRequest.Days, ignoreOrder: true);
    }

    [Fact]
    public async Task GivenExistingDiscount_WhenDeletesDiscount_ThenDiscountDeleted()
    {
        // 1. Sign in and create discount
        var authResponse = await Factory.AuthManager
            .CreateAndLoginUserAsync("b2b_discount_delete@example.com", "Password123!", UserType.B2B);
        Factory.ApiClient.SetAuth(authResponse);

        var companyId = (await Factory.ApiClient.PostAsync<CreateCompanyRequest, CreateCompanyResponse>(
            ApiRoutes.B2B.Company.Root, new CreateCompanyRequest("Disc Del Co", "dd@test.com"))).Id;
        var venueId = (await Factory.ApiClient.PostAsync<CreateVenueRequest, CreateVenueResponse>(
            ApiRoutes.B2B.Company.Venues(companyId),
            new CreateVenueRequest(0, 0, "DD Venue", null, null, null, null, null, null, []))).Id;
        var levelId = (await Factory.ApiClient.GetAsync<LoyaltyLevelDto[]>(
            ApiRoutes.B2B.Company.LoyaltyLevels(companyId)))[0].Id;

        var discountId = (await Factory.ApiClient.PostAsync<CreateDiscountRequest, CreateDiscountResponse>(
                ApiRoutes.B2B.Venue.Discounts(venueId), new CreateDiscountRequest(levelId, 5, [DayOfWeek.Monday])))
            .Id;

        // 2. Delete discount
        await Factory.ApiClient.DeleteAsync(
            ApiRoutes.B2B.Venue.Discount(venueId, discountId));

        // 3. Verify deleted
        var listResponse = await Factory.ApiClient.GetAsync<DiscountDto[]>(
            ApiRoutes.B2B.Venue.Discounts(venueId));
        listResponse.ShouldNotContain(d => d.Id == discountId.ToString());
    }

    [Fact]
    public async Task GivenAuthenticatedB2CUser_WhenAccessesDealsEndpoints_ThenReturnsForbidden()
    {
        // 1. Sign in as B2C user
        var authResponse = await Factory.AuthManager
            .CreateAndLoginUserAsync("b2c_intruder_deals@example.com", "Password123!");
        Factory.ApiClient.SetAuth(authResponse);

        var dummyId = Guid.NewGuid();

        // 2. Try various deals endpoints
        var promoResponse = await Factory.HttpClient.PostAsJsonAsync(
            ApiRoutes.B2B.Venue.Promotions(dummyId), new CreatePromotionRequest(PromoType.General, "Hacked"));
        promoResponse.StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        var discountResponse = await Factory.HttpClient.PostAsJsonAsync(
            ApiRoutes.B2B.Venue.Discounts(dummyId), new CreateDiscountRequest(0, 99, [DayOfWeek.Monday]));
        discountResponse.StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        var loyaltyResponse = await Factory.HttpClient.PutAsJsonAsync(
            ApiRoutes.B2B.Company.LoyaltyLevels(dummyId),
            new UpdateLoyaltyLevelsRequest(new[] { new LoyaltyLevelDto(0, 1, true) }));
        loyaltyResponse.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }
}