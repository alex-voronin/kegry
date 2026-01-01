using FeedModule.Domain.Aggregates;
using FeedModule.Domain.Repositories;
using Shared.Infrastructure.Persistence;

namespace FeedModule.Infrastructure.Persistence.Repositories;

public class EfCoreFilterRepository : Repository<FeedContext, Filter>, IFilterRepository
{
    public EfCoreFilterRepository(FeedContext context) : base(context)
    {
    }
}