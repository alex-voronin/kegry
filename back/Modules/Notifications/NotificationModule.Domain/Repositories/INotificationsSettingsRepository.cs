using System.Linq.Expressions;
using NotificationModule.Domain.Aggregates;
using SharedCore.Interfaces;

namespace NotificationModule.Domain.Repositories;

public interface INotificationsSettingsRepository : IRepository<NotificationsSettings>
{
    public Task<NotificationsSettings> GetByIdOrDefaultAsync(
        Guid id,
        bool asNoTracking = false,
        CancellationToken ct = default);

    public Task<(bool pushesEnabled, bool emailsEnabled)> GetNotificationsEnabledById(Guid id,
        CancellationToken ct = default);

    public Task<IReadOnlyDictionary<Guid, NotificationsSettings>> GetSettingsByIds(Guid[] ids,
        CancellationToken ct = default);
}