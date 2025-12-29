using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Persistence;
using UserProfileModule.Domain.Aggregates;

namespace UserProfileModule.Infrastructure.Persistence.Repositories;

internal class EfCoreHobbyRepository : Repository<UserProfileContext, Hobby>, IHobbyRepository
{
    public EfCoreHobbyRepository(UserProfileContext context) : base(context)
    {
    }

    public async Task<Hobby[]> GetAll(CancellationToken ct = default)
    {
        return await Context.Hobbies
            .OrderBy(h => h.Name)
            .ToArrayAsync(ct);
    }
}