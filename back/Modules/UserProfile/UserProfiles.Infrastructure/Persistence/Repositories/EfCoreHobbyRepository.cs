using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Persistence;
using UserProfiles.Domain.Aggregates;

namespace UserProfiles.Infrastructure.Persistence.Repositories;

public class EfCoreHobbyRepository : Repository<UserProfilesContext, Hobby>, IHobbyRepository
{
    public EfCoreHobbyRepository(UserProfilesContext context) : base(context)
    {
    }

    public async Task<Hobby[]> GetAll(CancellationToken ct = default)
    {
        return await Context.Hobbies
            .OrderBy(h => h.Name)
            .ToArrayAsync(ct);
    }
}