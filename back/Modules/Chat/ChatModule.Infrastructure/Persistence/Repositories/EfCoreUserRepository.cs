using ChatModule.Domain.Aggregates;
using ChatModule.Domain.Repositories;
using Shared.Infrastructure.Persistence;

namespace ChatModule.Infrastructure.Persistence.Repositories;

public class EfCoreUserRepository : Repository<ChatContext, User>, IUserRepository
{
    private readonly ISoftDeleteImplementer<ChatContext, User, Guid> _softDeleteImplementer;
    
    public EfCoreUserRepository(
        ChatContext context, 
        ISoftDeleteImplementer<ChatContext, User, Guid> softDeleteImplementer) : base(context)
    {
        _softDeleteImplementer = softDeleteImplementer;
    }

    public Task SoftDeleteAsync(Guid id, CancellationToken ct = default)
    {
        return _softDeleteImplementer.SoftDeleteAsync(id, ct);
    }
}
