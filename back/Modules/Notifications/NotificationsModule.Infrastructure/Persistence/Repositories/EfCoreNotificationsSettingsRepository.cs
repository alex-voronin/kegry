using NotificationModule.Domain.Aggregates;
using NotificationModule.Domain.Repositories;
using Shared.Infrastructure.Persistence;

namespace NotificationsModule.Infrastructure.Persistence.Repositories;

internal class EfCoreNotificationsSettingsRepository :
    Repository<NotificationsContext, NotificationsSettings>,
    INotificationsSettingsRepository
{
    private const bool DefaultPushesEnabled = true;
    private const bool DefaultEmailsEnabled = false;

    public EfCoreNotificationsSettingsRepository(NotificationsContext context) : base(context)
    {
    }

    public async Task<(bool pushesEnabled, bool emailsEnabled)> GetNotificationsEnabledById(
        Guid id,
        CancellationToken ct = default)
    {
        var notificationsSettings = await GetByIdAsync(id, asNoTracking: true, ct: ct);
        var pushesEnabled = notificationsSettings?.PushesEnabled ?? DefaultPushesEnabled;
        var emailsEnabled = notificationsSettings?.EmailsEnabled ?? DefaultEmailsEnabled;
        return (pushesEnabled, emailsEnabled);
    }
}