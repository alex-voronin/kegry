using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Persistence;
using VenueModule.Domain.Aggregates;
using VenueModule.Domain.Repositories;
using VenueModule.Infrastructure.Persistence;

namespace VenueModule.Infrastructure.Repositories;

public class EfCoreVisitRepository : Repository<VenueContext, Visit>, IVisitRepository
{
    private sealed class VisitGroupRow
    {
        [Column("venue_id")]
        public Guid VenueId { get; init; }

        [Column("group_date")]
        public DateTime GroupDate { get; init; }

        [Column("count")]
        public int Count { get; init; }
    }

    public EfCoreVisitRepository(VenueContext context) : base(context)
    {
    }

    public Task<int> GetVisitCountAsync(Guid userId, Guid venueId, CancellationToken ct = default)
    {
        return Context.Visits
            .Where(v => v.UserId == userId && v.VenueId == venueId)
            .CountAsync(ct);
    }

    public async Task<List<(Guid VenueId, DateTime GroupDate, int Count)>> GetVisitsGroupedAsync(
        List<Guid> venueIds,
        VisitsGrouping grouping,
        DateTime since,
        CancellationToken ct)
    {
        if (venueIds.Count == 0)
            return [];

        var truncUnit = grouping switch
        {
            VisitsGrouping.Daily => "day",
            VisitsGrouping.Weekly => "week",
            VisitsGrouping.Monthly => "month",
            _ => throw new ArgumentOutOfRangeException(nameof(grouping), grouping, null)
        };

        var truncate = $"DATE_TRUNC('{truncUnit}', v.checked_in_at AT TIME ZONE 'UTC')";
        var sql = $"""
                   SELECT v.venue_id, {truncate} as group_date, COUNT(*) 
                   FROM venue.visits AS v WHERE v.venue_id = ANY(@p0) AND v.checked_in_at >= @p1 
                   GROUP BY v.venue_id, {truncate}
                   """;
        var rawResult = await Context.Database
            .SqlQueryRaw<VisitGroupRow>(sql, venueIds.ToArray(), since)
            .ToListAsync(ct);

        return rawResult
            .Select(r => (r.VenueId, DateTime.SpecifyKind(r.GroupDate, DateTimeKind.Utc), r.Count))
            .ToList();
    }
}