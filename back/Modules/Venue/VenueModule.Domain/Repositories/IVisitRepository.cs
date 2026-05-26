using SharedCore.Interfaces;
using VenueModule.Domain.Aggregates;

namespace VenueModule.Domain.Repositories;

public enum VisitsGrouping
{
    Daily,
    Weekly,
    Monthly,
}

public interface IVisitRepository : IRepository<Visit>
{
    Task<int> GetVisitCountAsync(Guid userId, Guid venueId, CancellationToken ct = default);

    Task<List<(Guid VenueId, DateTime GroupDate, int Count)>> GetVisitsGroupedAsync(
        List<Guid> venueIds,
        VisitsGrouping grouping,
        DateTime since,
        CancellationToken ct);
}
