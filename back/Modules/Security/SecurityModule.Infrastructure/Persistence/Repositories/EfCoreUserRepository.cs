using Microsoft.EntityFrameworkCore;
using SecurityModule.Domain.Aggregates;
using SecurityModule.Domain.Repositories;
using Shared.Infrastructure.Persistence;

namespace SecurityModule.Infrastructure.Persistence.Repositories;

public class EfCoreUserRepository : Repository<SecurityContext, User>, IUserRepository
{
    public EfCoreUserRepository(SecurityContext context) : base(context)
    {
    }

    public Task<bool> IsUserExistAsync(string email, CancellationToken ct = default)
    {
        return Context.Users.AnyAsync(u => u.NormalizedEmail == email.ToUpperInvariant(), ct);
    }
}