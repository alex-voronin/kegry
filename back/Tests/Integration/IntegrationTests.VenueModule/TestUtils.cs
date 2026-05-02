using IntegrationTests.Shared;
using Microsoft.Extensions.DependencyInjection;
using SecurityModule.API.Dto;
using SharedCore.Api.Dto;
using Shouldly;
using VenueModule.API.B2B.Companies.Dto;
using VenueModule.API.B2B.Deals.Dto;
using VenueModule.API.B2B.Venues.Dto;
using VenueModule.API.B2C.Dto;
using VenueModule.Domain.Repositories;
using VenueModule.Domain.ValueObjects;
using VenueModule.Infrastructure.Persistence;

namespace IntegrationTests.VenueModule;

public class TestUtils
{
    private readonly IntegrationWebApplicationFactory _factory;

    public TestUtils(IntegrationWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public async Task<Guid> CreateCompanyAsync(string name, string email)
    {
        var response = await _factory.ApiClient.PostAsync<CreateCompanyRequest, ApiResult<CreateCompanyResponse>>(
            ApiRoutes.B2B.Company.Root, new CreateCompanyRequest(name, email));
        response.Success.ShouldBeTrue();
        return response.Result.Id;
    }

    public async Task<Guid> CreateVerifiedVenueAsync(Guid companyId, double lat, double lon, string address)
    {
        var response = await _factory.ApiClient.PostAsync<CreateVenueRequest, ApiResult<CreateVenueResponse>>(
            ApiRoutes.B2B.Company.Venues(companyId),
            new CreateVenueRequest(lat, lon, address, null, null, null, null, null, null));
        response.Success.ShouldBeTrue();
        var venueId = response.Result.Id;

        await VerifyVenueByIdAsync(venueId);
        return venueId;
    }

    public async Task VerifyVenueByIdAsync(Guid venueId)
    {
        using var scope = _factory.Services.CreateScope();
        var venueRepository = scope.ServiceProvider.GetService<IVenueRepository>()!;
        var v = await venueRepository.GetByIdAsync(venueId, ignoreFilters: [NamedFilters.Venue.Verified]);
        v!.IsVerified = true;
        await venueRepository.SaveChangesAsync();
    }

    public async Task AddDiscountAsync(Guid venueId, Guid companyId, int percent)
    {
        var levels = await _factory.ApiClient.GetAsync<ApiResult<LoyaltyLevelDto[]>>(
            ApiRoutes.B2B.Company.LoyaltyLevels(companyId));
        levels.Success.ShouldBeTrue();

        var response = await _factory.ApiClient.PostAsync<CreateDiscountRequest, ApiResult<CreateDiscountResponse>>(
            ApiRoutes.B2B.Venue.Discounts(venueId),
            new CreateDiscountRequest(levels.Result[0].Id, percent,
            [
                DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday,
                DayOfWeek.Saturday, DayOfWeek.Sunday
            ]));
        response.Success.ShouldBeTrue();
    }

    public async Task AddPromotionAsync(Guid venueId, string body)
    {
        var response = await _factory.ApiClient.PostAsync<CreatePromotionRequest, ApiResult<CreatePromotionResponse>>(
            ApiRoutes.B2B.Venue.Promotions(venueId),
            new CreatePromotionRequest(PromoType.General, body));
        response.Success.ShouldBeTrue();
    }

    public async Task<(Guid companyId, Guid venueId)> SetupLoyaltyLevelsAndCreateDiscounts(LoyaltyLevelDto[] levels,
        CreateDiscountRequest[] discounts)
    {
        var companyId = await CreateCompanyAsync("Tiered B2C Co", "tiered_b2c@example.com");
        var venueId = await CreateVerifiedVenueAsync(companyId, 0, 0, "Address");

        // 2. Configure Loyalty Levels
        foreach (var level in levels)
        {
            var response = await _factory.ApiClient.PutAsync<UpdateLoyaltyLevelRequest, ApiResult.EmptyResult>(
                ApiRoutes.B2B.Company.LoyaltyLevels(companyId),
                new UpdateLoyaltyLevelRequest(level));
            response.Success.ShouldBeTrue();
        }

        // 3. Create discounts
        foreach (var discount in discounts)
        {
            var response = await _factory.ApiClient.PostAsync<CreateDiscountRequest, ApiResult<CreateDiscountResponse>>(
                ApiRoutes.B2B.Venue.Discounts(venueId), discount);
            response.Success.ShouldBeTrue();
        }

        return (companyId, venueId);
    }

    public async Task<AuthResponse> CreateB2CUserAndReachLoyaltyLevel(
        string email, string password,
        LoyaltyLevelDto[] levels, int targetLevel,
        Guid venueId)
    {
        // 1. Create B2C User
        var b2CAuth = await _factory.AuthManager.CreateAndLoginUserAsync(email, password);
        _factory.ApiClient.SetAuth(b2CAuth);

        // 2. Perform 2 check-ins to reach Level 2 (starts at 2 visits)
        var now = DateTimeOffset.UtcNow;
        var index = Math.Max(targetLevel - 1, 0);
        for (var i = 0; i < levels[index].VisitsUpperBound; i++)
        {
            var checkInResponse = await _factory.ApiClient.PostAsync<CheckInRequest, ApiResult<CheckInResponse>>(
                ApiRoutes.Venue.QrCheckIn(venueId), new CheckInRequest(0, 0, null));
            checkInResponse.Success.ShouldBeTrue();

            // Advance time to allow next check-in
            _factory.FakeTimeProvider.Advance(TimeSpan.FromDays(1));
        }

        _factory.FakeTimeProvider.Reset(now);
        return b2CAuth;
    }
}