using FeedModule.Domain.Aggregates;
using FeedModule.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Persistence;
using SharedCore.Services.LifeTimes;
using Wolverine.EntityFrameworkCore;

namespace FeedModule.Infrastructure.Persistence.Repositories;

internal class EfCoreFeedProfileRepository : RepositoryOutbox<FeedContext, FeedProfile>, IFeedProfileRepository, IScoped
{
    private readonly IDbContextOutbox<FeedContext> _contextOutbox;
    
    public EfCoreFeedProfileRepository(IDbContextOutbox<FeedContext> contextOutbox) : base(contextOutbox)
    {
        _contextOutbox = contextOutbox;
    }
    
    public async Task<FeedProfile?> GetAsync(Guid myUserId, Guid otherUserId, bool asNoTracking = false, CancellationToken ct = default)
    {
        var query = Context.FeedProfiles.AsQueryable();
        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync(fp => fp.MyUserId == myUserId && fp.OtherUserId == otherUserId, ct);
    }

    public async Task<bool> IsInMeetingAsync(Guid userId, CancellationToken ct = default)
    {
        return await Context.FeedProfiles.AnyAsync(fp => fp.MyUserId == userId && fp.IsInMeeting, ct);
    }
}
