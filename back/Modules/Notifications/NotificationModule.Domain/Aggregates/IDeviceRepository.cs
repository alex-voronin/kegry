using SharedCore.Interfaces;

namespace NotificationModule.Domain.Aggregates;

public interface IDeviceRepository : IRepository<Device>
{
    Task<Device[]> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task DeleteByFcmTokenAsync(string fcmToken, CancellationToken ct = default);
}
