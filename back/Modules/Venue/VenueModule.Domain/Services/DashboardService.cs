using SharedCore.Services.LifeTimes;
using VenueModule.Domain.Repositories;

namespace VenueModule.Domain.Services;

public record GroupedVisitsData(
    Dictionary<Guid, Dictionary<DateTime, int>> VisitsByVenue,
    DateTime Since,
    int DataPointsCount);

public interface IDashboardService
{
    Task<GroupedVisitsData> LoadGroupedVisitsAsync(
        List<Guid> venueIds, VisitsGrouping grouping, CancellationToken ct);
}

public class DashboardService : IDashboardService, IScoped
{
    private const int DataPointsCount = 12;

    private readonly IVisitRepository _visitRepository;

    public DashboardService(IVisitRepository visitRepository)
    {
        _visitRepository = visitRepository;
    }

    public async Task<GroupedVisitsData> LoadGroupedVisitsAsync(
        List<Guid> venueIds, VisitsGrouping grouping, CancellationToken ct)
    {
        var since = GetSince(grouping);

        var rawData = await _visitRepository.GetVisitsGroupedAsync(
            venueIds, grouping, since, ct);

        var visitsByVenue = rawData
            .GroupBy(x => x.VenueId)
            .ToDictionary(
                g => g.Key,
                g => g.ToDictionary(x => x.GroupDate, x => x.Count));

        return new GroupedVisitsData(visitsByVenue, since, DataPointsCount);
    }

    private static DateTime GetSince(VisitsGrouping grouping)
    {
        var now = DateTime.UtcNow;
        var weekStart = GetStartOfThisWeek();
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var historicalDataPointsCount = DataPointsCount - 1; // as the last one is today/this week/this month
        return grouping switch
        {
            VisitsGrouping.Daily => now.Date.AddDays(-historicalDataPointsCount),
            VisitsGrouping.Weekly => weekStart.AddDays(-(7 * historicalDataPointsCount)),
            VisitsGrouping.Monthly => monthStart.AddMonths(-historicalDataPointsCount),
            _ => throw new ArgumentOutOfRangeException(nameof(grouping))
        };
    }

    private static DateTime GetStartOfThisWeek()
    {
        var today = DateTime.UtcNow.Date;
        var diff = (7 + (today.DayOfWeek - DayOfWeek.Monday)) % 7; // trick to make monday the first day of week
        return today.AddDays(-diff);
    }
}
