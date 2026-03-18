using SharedCore.Interfaces;
using VenueModule.Domain.Aggregates;

namespace VenueModule.Domain.Repositories;

public interface IUserLoyaltyRepository : IRepository<UserLoyalty>
{
    Task<UserLoyalty?> GetByUserAndCompanyId(Guid userId, Guid companyId, CancellationToken ct = default);
}