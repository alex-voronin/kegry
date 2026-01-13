using Shared.Infrastructure.Persistence;
using UserProfileModule.Domain.Aggregates;
using UserProfileModule.Domain.Repositories;
using Wolverine.EntityFrameworkCore;

namespace UserProfileModule.Infrastructure.Persistence.Repositories;

internal class EfCoreUserRepository : RepositoryOutbox<UserProfileContext, User>, IUserRepository
{
    private readonly ISoftDeleteImplementer<UserProfileContext, User, Guid> _softDeleteImplementer;

    public EfCoreUserRepository(
        IDbContextOutbox<UserProfileContext> contextOutbox,
        ISoftDeleteImplementer<UserProfileContext, User, Guid> softDeleteImplementer) : base(contextOutbox)
    {
        _softDeleteImplementer = softDeleteImplementer;
    }

    public Task SoftDeleteAsync(Guid id, CancellationToken ct = default)
    {
        return _softDeleteImplementer.SoftDeleteAsync(id, ct);
    }
}