using SharedCore.Interfaces;
using VenueModule.Domain.Aggregates;

namespace VenueModule.Domain.Repositories;

public interface IVisitRepository : IRepository<Visit>
{
    Task<Visit?> GetLastVisit(Guid userId, Guid venueId);
}