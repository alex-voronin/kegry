using NotificationModule.Domain.Aggregates;
using SharedCore.Interfaces;

namespace NotificationModule.Domain.Repositories;

public interface INotificationRepository : IRepository<Notification>
{
    Task<(Notification[] Items, int TotalCount)> GetByUserIdPaginatedAsync(Guid userId, int offset, int limit, CancellationToken ct);
    Task MarkAsSeenAsync(IEnumerable<Guid> notificationIds, CancellationToken ct);
    Task<int> GetUnseenCountAsync(Guid userId, CancellationToken ct);
}
