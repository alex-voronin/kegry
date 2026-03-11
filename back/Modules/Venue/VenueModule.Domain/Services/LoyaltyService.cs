using SharedCore.Services.LifeTimes;
using VenueModule.Domain.Repositories;

namespace VenueModule.Domain.Services;

public interface ILoyaltyService
{
    Task<int> GetLoyaltyLevelAsync(Guid userId, Guid venueId, CancellationToken ct = default);
}

public class LoyaltyService : ILoyaltyService, IScoped
{
    private readonly IVisitRepository _visitRepository;

    public LoyaltyService(IVisitRepository visitRepository)
    {
        _visitRepository = visitRepository;
    }

    public async Task<int> GetLoyaltyLevelAsync(Guid userId, Guid venueId, CancellationToken ct = default)
    {
        var visits = await _visitRepository.GetVisitCountAsync(userId, venueId, ct);
        return visits > 999 ? 999 : visits;
    }
}