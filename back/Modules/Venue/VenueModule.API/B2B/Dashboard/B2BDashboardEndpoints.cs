using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using SharedCore.Api;
using SharedCore.Extensions;
using SharedCore.Policies;
using VenueModule.API.B2B.Dashboard.Dto;
using VenueModule.Domain.Aggregates;
using VenueModule.Domain.Exceptions;
using VenueModule.Domain.Repositories;
using VenueModule.Domain.Services;

namespace VenueModule.API.B2B.Dashboard;

public class B2BDashboardEndpoints : IEndpoints
{
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("")
            .WithTags("Dashboard API")
            .RequireAuthorization(AuthPolicies.B2BAdmin);

        group.MapGet("/api/v1/b2b/dashboard/visits", GetVisitsChart);
    }

    private static async Task<VisitsChartResponseDto> GetVisitsChart(
        [FromQuery] Guid companyId,
        [FromQuery] VisitsChartMode mode,
        ClaimsPrincipal user,
        ICompanyRepository companyRepository,
        IDashboardService dashboardService,
        CancellationToken ct)
    {
        var ownerId = user.GetId();

        var company = await companyRepository.GetByIdAsync(
            companyId: companyId,
            ownerId: ownerId,
            asNoTracking: true,
            includeVenues: true,
            ignoreVerifiedFilter: true,
            ct: ct);

        if (company == null)
        {
            throw VenueExceptions.CompanyNotFound();
        }

        var activeVenueIds = company.Venues
            .Where(v => !v.IsDeleted)
            .Select(v => v.Id)
            .ToList();

        if (activeVenueIds.Count == 0)
            return new VisitsChartResponseDto([]);

        var grouping = mode switch
        {
            VisitsChartMode.Daily => VisitsGrouping.Daily,
            VisitsChartMode.Weekly => VisitsGrouping.Weekly,
            VisitsChartMode.Monthly => VisitsGrouping.Monthly,
            _ => throw new ArgumentOutOfRangeException(nameof(mode))
        };

        var data = await dashboardService.LoadGroupedVisitsAsync(
            activeVenueIds, grouping, ct);

        var venueDtos = company.Venues
            .Where(v => !v.IsDeleted)
            .Select(venue => CreateVenueVisitsDto(venue, data.VisitsByVenue, data.Since, data.DataPointsCount, mode))
            .ToArray();

        return new VisitsChartResponseDto(venueDtos);
    }

    private static VenueVisitsDto CreateVenueVisitsDto(
        Venue venue,
        Dictionary<Guid, Dictionary<DateTime, int>> visitsByVenue,
        DateTime since,
        int dataPointsCount,
        VisitsChartMode mode)
    {
        var venueData = visitsByVenue.GetValueOrDefault(venue.Id);

        var dataPoints = GenerateDataPointKeys(since, mode, dataPointsCount)
            .Select(date =>
            {
                var count = venueData?.TryGetValue(date, out var c) == true ? c : 0;
                return new VisitsChartDataPointDto(date.ToString("yyyy-MM-dd"), count);
            })
            .ToArray();

        return new VenueVisitsDto(
            venue.Id.ToString(),
            venue.Address,
            dataPoints);
    }

    private static DateTime[] GenerateDataPointKeys(DateTime since, VisitsChartMode mode, int dataPointsCount)
    {
        return Enumerable.Range(0, dataPointsCount)
            .Select(i => mode switch
            {
                VisitsChartMode.Daily => since.AddDays(i),
                VisitsChartMode.Weekly => since.AddDays(i * 7),
                VisitsChartMode.Monthly => since.AddMonths(i),
                _ => since
            })
            .ToArray();
    }
}