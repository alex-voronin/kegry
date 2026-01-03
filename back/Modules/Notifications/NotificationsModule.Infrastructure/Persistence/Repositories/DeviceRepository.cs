using Microsoft.EntityFrameworkCore;
using NotificationModule.Domain.Aggregates;
using Shared.Infrastructure.Persistence;

namespace NotificationsModule.Infrastructure.Persistence.Repositories;

internal class DeviceRepository : Repository<NotificationsContext, Device>, IDeviceRepository
{
    public DeviceRepository(NotificationsContext context) : base(context)
    {
    }

    public Task<Device[]> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        return Context.Devices
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .ToArrayAsync(ct);
    }

    public async Task DeleteByFcmTokenAsync(string fcmToken, CancellationToken ct = default)
    {
        await Context.Devices
            .Where(u => u.FcmToken == fcmToken)
            .ExecuteDeleteAsync(ct);
    }
}