using Microsoft.EntityFrameworkCore;
using NotificationModule.Domain.Aggregates;
using NotificationModule.Domain.Repositories;
using Shared.Infrastructure.Persistence;

namespace NotificationsModule.Infrastructure.Persistence.Repositories;

internal class EfCoreNotificationRepository : Repository<NotificationsContext, Notification>, INotificationRepository
{
    public EfCoreNotificationRepository(NotificationsContext context) : base(context)
    {
    }

    public async Task<(Notification[] Items, int TotalCount)> GetByUserIdPaginatedAsync(Guid userId, int offset, int limit, CancellationToken ct)
    {
        var query = Context.Notifications
            .AsNoTracking()
            .Include(x => x.FromUser)
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt);

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .Skip(offset)
            .Take(limit)
            .ToArrayAsync(ct);

        return (items, totalCount);
    }

    public async Task MarkAsSeenAsync(IEnumerable<Guid> notificationIds, CancellationToken ct)
    {
        await Context.Notifications
            .Where(x => notificationIds.Contains(x.Id))
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.IsSeen, true), ct);
    }

    public Task<int> GetUnseenCountAsync(Guid userId, CancellationToken ct)
    {
        return Context.Notifications
            .CountAsync(x => x.UserId == userId && !x.IsSeen, ct);
    }
}
