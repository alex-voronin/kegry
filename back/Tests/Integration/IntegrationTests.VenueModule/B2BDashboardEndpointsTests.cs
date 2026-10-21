using System.Net;
using IntegrationTests.Shared;
using Microsoft.Extensions.DependencyInjection;
using SecurityModule.Domain.ValueObjects;
using SharedCore.Exceptions;
using Shouldly;
using VenueModule.API.B2B.Companies.Dto;
using VenueModule.API.B2B.Dashboard.Dto;
using VenueModule.API.B2B.Venues.Dto;
using VenueModule.Domain.Aggregates;
using VenueModule.Infrastructure.Persistence;

namespace IntegrationTests.VenueModule;

[Collection("IntegrationTests")]
public class B2BDashboardEndpointsTests : IntegrationTestBase, IAsyncLifetime
{
    public B2BDashboardEndpointsTests(IntegrationWebApplicationFactory factory) : base(factory)
    {
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public Task DisposeAsync() => ContainerManager.ResetDatabaseAsync();

    [Fact]
    public async Task GivenB2BUserWithVenue_WhenDailyModeNoVisits_ThenAllZeroDataPoints()
    {
        var authResponse = await Factory.AuthManager
            .CreateAndLoginUserAsync("b2b_dash_novisits@example.com", "Password123!", UserType.B2B);
        Factory.ApiClient.SetAuth(authResponse);

        var companyId = (await Factory.ApiClient.PostAsync<CreateCompanyRequest, CreateCompanyResponse>(
            ApiRoutes.B2B.Company.Root, new CreateCompanyRequest("No Visits Co", "novisits@test.com"))).Id;

        await Factory.ApiClient.PostAsync<CreateVenueRequest, CreateVenueResponse>(
            ApiRoutes.B2B.Company.Venues(companyId),
            new CreateVenueRequest(0, 0, "NV Venue", null, null, null, null, null, null, []));

        var response = await Factory.ApiClient.GetAsync<VisitsChartResponseDto>(
            DashboardVisitsUrl(companyId, "Daily"));

        response.Venues.Length.ShouldBe(1);
        response.Venues[0].DataPoints.Length.ShouldBe(12);
        response.Venues[0].DataPoints.ShouldAllBe(dp => dp.Count == 0);
    }

    [Fact]
    public async Task GivenB2BUserWithVisits_WhenDailyMode_ThenCountsMatchBuckets()
    {
        var b2BAuth = await Factory.AuthManager
            .CreateAndLoginUserAsync("b2b_dash_daily@example.com", "Password123!", UserType.B2B);
        Factory.ApiClient.SetAuth(b2BAuth);

        var companyId = (await Factory.ApiClient.PostAsync<CreateCompanyRequest, CreateCompanyResponse>(
            ApiRoutes.B2B.Company.Root, new CreateCompanyRequest("Daily Vis Co", "dailyvis@test.com"))).Id;

        var venueId = (await Factory.ApiClient.PostAsync<CreateVenueRequest, CreateVenueResponse>(
            ApiRoutes.B2B.Company.Venues(companyId),
            new CreateVenueRequest(0, 0, "DV Venue", null, null, null, null, null, null, []))).Id;

        var b2CAuth = await Factory.AuthManager
            .CreateAndLoginUserAsync("b2c_dash_daily@example.com", "Password123!");
        var b2CUserId = Guid.Parse(b2CAuth.UserId);

        var today = DateTime.UtcNow.Date;
        await SeedVisitAsync(venueId, b2CUserId, today.AddHours(10));
        await SeedVisitAsync(venueId, b2CUserId, today.AddDays(-1).AddHours(14));
        await SeedVisitAsync(venueId, b2CUserId, today.AddDays(-3).AddHours(9));
        await SeedVisitAsync(venueId, b2CUserId, today.AddDays(-3).AddHours(18));

        Factory.ApiClient.SetAuth(b2BAuth);
        var response = await Factory.ApiClient.GetAsync<VisitsChartResponseDto>(
            DashboardVisitsUrl(companyId, "Daily"));

        var venueData = response.Venues.ShouldHaveSingleItem();
        venueData.DataPoints.Length.ShouldBe(12);
        venueData.DataPoints.ShouldContain(dp => dp.Label == today.ToString("yyyy-MM-dd") && dp.Count == 1);
        venueData.DataPoints.ShouldContain(dp => dp.Label == today.AddDays(-1).ToString("yyyy-MM-dd") && dp.Count == 1);
        venueData.DataPoints.ShouldContain(dp => dp.Label == today.AddDays(-3).ToString("yyyy-MM-dd") && dp.Count == 2);
    }

    [Fact]
    public async Task GivenB2BUserWithVisits_WhenWeeklyMode_ThenCountsMatchBuckets()
    {
        var b2BAuth = await Factory.AuthManager
            .CreateAndLoginUserAsync("b2b_dash_weekly@example.com", "Password123!", UserType.B2B);
        Factory.ApiClient.SetAuth(b2BAuth);

        var companyId = (await Factory.ApiClient.PostAsync<CreateCompanyRequest, CreateCompanyResponse>(
            ApiRoutes.B2B.Company.Root, new CreateCompanyRequest("Weekly Vis Co", "weeklyvis@test.com"))).Id;

        var venueId = (await Factory.ApiClient.PostAsync<CreateVenueRequest, CreateVenueResponse>(
            ApiRoutes.B2B.Company.Venues(companyId),
            new CreateVenueRequest(0, 0, "WV Venue", null, null, null, null, null, null, []))).Id;

        var b2CAuth = await Factory.AuthManager
            .CreateAndLoginUserAsync("b2c_dash_weekly@example.com", "Password123!");
        var b2CUserId = Guid.Parse(b2CAuth.UserId);

        var today = DateTime.UtcNow.Date;
        var monday = today.AddDays(-(int)today.DayOfWeek + (int)DayOfWeek.Monday);
        if (monday > today) monday = monday.AddDays(-7);

        await SeedVisitAsync(venueId, b2CUserId, monday.AddHours(12));
        await SeedVisitAsync(venueId, b2CUserId, monday.AddDays(-7).AddHours(10));
        await SeedVisitAsync(venueId, b2CUserId, monday.AddDays(-7).AddHours(14));
        await SeedVisitAsync(venueId, b2CUserId, monday.AddDays(-14).AddHours(9));

        Factory.ApiClient.SetAuth(b2BAuth);
        var response = await Factory.ApiClient.GetAsync<VisitsChartResponseDto>(
            DashboardVisitsUrl(companyId, "Weekly"));

        var venueData = response.Venues.ShouldHaveSingleItem();
        venueData.DataPoints.Length.ShouldBe(12);
        venueData.DataPoints.ShouldContain(dp =>
            dp.Label == monday.ToString("yyyy-MM-dd") && dp.Count == 1);
        venueData.DataPoints.ShouldContain(dp =>
            dp.Label == monday.AddDays(-7).ToString("yyyy-MM-dd") && dp.Count == 2);
        venueData.DataPoints.ShouldContain(dp =>
            dp.Label == monday.AddDays(-14).ToString("yyyy-MM-dd") && dp.Count == 1);
    }

    [Fact]
    public async Task GivenB2BUserWithVisits_WhenMonthlyMode_ThenCountsMatchBuckets()
    {
        var b2BAuth = await Factory.AuthManager
            .CreateAndLoginUserAsync("b2b_dash_monthly@example.com", "Password123!", UserType.B2B);
        Factory.ApiClient.SetAuth(b2BAuth);

        var companyId = (await Factory.ApiClient.PostAsync<CreateCompanyRequest, CreateCompanyResponse>(
            ApiRoutes.B2B.Company.Root, new CreateCompanyRequest("Monthly Vis Co", "monthlyvis@test.com"))).Id;

        var venueId = (await Factory.ApiClient.PostAsync<CreateVenueRequest, CreateVenueResponse>(
            ApiRoutes.B2B.Company.Venues(companyId),
            new CreateVenueRequest(0, 0, "MV Venue", null, null, null, null, null, null, []))).Id;

        var b2CAuth = await Factory.AuthManager
            .CreateAndLoginUserAsync("b2c_dash_monthly@example.com", "Password123!");
        var b2CUserId = Guid.Parse(b2CAuth.UserId);

        var today = DateTime.UtcNow.Date;
        var thisMonth = new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        await SeedVisitAsync(venueId, b2CUserId, thisMonth.AddDays(5).AddHours(10));
        await SeedVisitAsync(venueId, b2CUserId, thisMonth.AddMonths(-1).AddDays(10).AddHours(12));
        await SeedVisitAsync(venueId, b2CUserId, thisMonth.AddMonths(-1).AddDays(20).AddHours(14));
        await SeedVisitAsync(venueId, b2CUserId, thisMonth.AddMonths(-2).AddDays(15).AddHours(9));

        Factory.ApiClient.SetAuth(b2BAuth);
        var response = await Factory.ApiClient.GetAsync<VisitsChartResponseDto>(
            DashboardVisitsUrl(companyId, "Monthly"));

        var venueData = response.Venues.ShouldHaveSingleItem();
        venueData.DataPoints.Length.ShouldBe(12);
        venueData.DataPoints.ShouldContain(dp =>
            dp.Label == thisMonth.ToString("yyyy-MM-dd") && dp.Count == 1);
        venueData.DataPoints.ShouldContain(dp =>
            dp.Label == thisMonth.AddMonths(-1).ToString("yyyy-MM-dd") && dp.Count == 2);
        venueData.DataPoints.ShouldContain(dp =>
            dp.Label == thisMonth.AddMonths(-2).ToString("yyyy-MM-dd") && dp.Count == 1);
    }

    [Fact]
    public async Task GivenB2BUser_WhenCompanyHasNoVenues_ThenReturnsEmptyVenues()
    {
        var authResponse = await Factory.AuthManager
            .CreateAndLoginUserAsync("b2b_dash_novenues@example.com", "Password123!", UserType.B2B);
        Factory.ApiClient.SetAuth(authResponse);

        var companyId = (await Factory.ApiClient.PostAsync<CreateCompanyRequest, CreateCompanyResponse>(
            ApiRoutes.B2B.Company.Root, new CreateCompanyRequest("No Venues Co", "novenues@test.com"))).Id;

        var response = await Factory.ApiClient.GetAsync<VisitsChartResponseDto>(
            DashboardVisitsUrl(companyId, "Daily"));

        response.Venues.ShouldBeEmpty();
    }

    [Fact]
    public async Task GivenB2BUser_WhenCompanyNotOwned_ThenReturnsError()
    {
        var authA = await Factory.AuthManager
            .CreateAndLoginUserAsync("b2b_dash_owner_a@example.com", "Password123!", UserType.B2B);
        Factory.ApiClient.SetAuth(authA);

        var companyId = (await Factory.ApiClient.PostAsync<CreateCompanyRequest, CreateCompanyResponse>(
            ApiRoutes.B2B.Company.Root, new CreateCompanyRequest("Owner A Co", "ownera@test.com"))).Id;

        var authB = await Factory.AuthManager
            .CreateAndLoginUserAsync("b2b_dash_owner_b@example.com", "Password123!", UserType.B2B);
        Factory.ApiClient.SetAuth(authB);

        var error = await Factory.ApiClient.GetProblemAsync(
            DashboardVisitsUrl(companyId, "Daily"));

        error.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        error.ProblemDetails.Status.ShouldBe((int)HttpStatusCode.NotFound);
        error.ProblemDetails.GetCode().ShouldBe(nameof(ErrorCode.NotFound));
    }

    [Fact]
    public async Task GivenB2CUser_WhenAccessesDashboard_ThenReturnsForbidden()
    {
        var authResponse = await Factory.AuthManager
            .CreateAndLoginUserAsync("b2c_dash_forbidden@example.com", "Password123!");
        Factory.ApiClient.SetAuth(authResponse);

        var dummyCompanyId = Guid.NewGuid();
        var httpResponse = await Factory.HttpClient.GetAsync(
            DashboardVisitsUrl(dummyCompanyId, "Daily"));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    private static string DashboardVisitsUrl(Guid companyId, string mode) =>
        $"/api/v1/b2b/dashboard/visits?companyId={companyId}&mode={mode}";

    private async Task SeedVisitAsync(Guid venueId, Guid userId, DateTime checkedInAtUtc)
    {
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<VenueContext>();
        context.Visits.Add(new Visit
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            VenueId = venueId,
            CheckedInAt = new DateTimeOffset(checkedInAtUtc, TimeSpan.Zero)
        });
        await context.SaveChangesAsync();
    }
}
