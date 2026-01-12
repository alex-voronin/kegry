using NotificationModule.Domain.Aggregates;
using SharedCore.Interfaces;

namespace NotificationModule.Domain.Repositories;

public interface INotificationsSettingsRepository : IRepository<NotificationsSettings>
{
    public Task<(bool pushesEnabled, bool emailsEnabled)> GetNotificationsEnabledById(Guid id, CancellationToken ct = default);
}
