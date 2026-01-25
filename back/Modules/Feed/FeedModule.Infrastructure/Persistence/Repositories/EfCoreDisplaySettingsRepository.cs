using FeedModule.Domain.Aggregates;
using FeedModule.Domain.Repositories;
using Shared.Infrastructure.Persistence;

namespace FeedModule.Infrastructure.Persistence.Repositories;

public class EfCoreDisplaySettingsRepository : Repository<FeedContext, DisplaySettings>, IDisplaySettingsRepository
{
    public EfCoreDisplaySettingsRepository(FeedContext context) : base(context)
    {
    }
}