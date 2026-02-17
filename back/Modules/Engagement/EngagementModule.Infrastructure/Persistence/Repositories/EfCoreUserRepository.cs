using EngagementModule.Domain.Aggregates;
using EngagementModule.Domain.Repositories;
using Shared.Infrastructure.Persistence;

namespace EngagementModule.Infrastructure.Persistence.Repositories;

public class EfCoreUserRepository : Repository<EngagementContext, User>, IUserRepository
{
    private readonly ISoftDeleteImplementer<EngagementContext, User, Guid> _softDeleteImplementer;

    public EfCoreUserRepository(
        EngagementContext context,
        ISoftDeleteImplementer<EngagementContext, User, Guid> softDeleteImplementer) : base(context)
    {
        _softDeleteImplementer = softDeleteImplementer;
    }

    public Task SoftDeleteAsync(Guid id, CancellationToken ct = default)
    {
        return _softDeleteImplementer.SoftDeleteAsync(id, ct);
    }
}