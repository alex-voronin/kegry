using Microsoft.EntityFrameworkCore;
using NotificationModule.Domain.Aggregates;
using NotificationModule.Domain.Repositories;
using Shared.Infrastructure.Persistence;

namespace NotificationsModule.Infrastructure.Persistence.Repositories;

internal class EfCoreDeviceRepository : Repository<NotificationsContext, Device>, IDeviceRepository
{
    public EfCoreDeviceRepository(NotificationsContext context) : base(context)
    {
    }

    public Task<Device[]> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        return Context.Devices
            .AsNoTracking()
            .Where(d => d.UserId == userId)
            .ToArrayAsync(ct);
    }

    public Task DeleteByFcmTokenAsync(string fcmToken, CancellationToken ct = default)
    {
        return Context.Devices
            .Where(d => d.FcmToken == fcmToken)
            .ExecuteDeleteAsync(ct);
    }

    public Task DeleteByIdAsync(string id, Guid userId, CancellationToken ct = default)
    {
        return Context.Devices
            .Where(d => d.Id == id && d.UserId == userId)
            .ExecuteDeleteAsync(ct);
    }

    public Task DeleteByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        return Context.Devices
            .Where(d => d.UserId == userId)
            .ExecuteDeleteAsync(ct);
    }
}