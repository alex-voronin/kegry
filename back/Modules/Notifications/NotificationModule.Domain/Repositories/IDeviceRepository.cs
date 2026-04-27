using NotificationModule.Domain.Aggregates;
using SharedCore.Interfaces;

namespace NotificationModule.Domain.Repositories;

public interface IDeviceRepository : IRepository<Device>
{
    Task<Device[]> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task DeleteByFcmTokenAsync(string fcmToken, CancellationToken ct = default);
    Task DeleteByIdAsync(string id, Guid userId, CancellationToken ct = default);
    Task DeleteByUserIdAsync(Guid userId, CancellationToken ct = default);
}
