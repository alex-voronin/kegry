using Microsoft.EntityFrameworkCore;
using NotificationModule.Domain.Aggregates;
using NotificationModule.Domain.Repositories;
using NotificationModule.Domain.ValueObjects;
using Shared.Infrastructure.Persistence;

namespace NotificationsModule.Infrastructure.Persistence.Repositories;

internal class EfCoreNotificationsSettingsRepository :
    Repository<NotificationsContext, NotificationsSettings>,
    INotificationsSettingsRepository
{
    private const bool DefaultPushesEnabled = true;
    private const bool DefaultEmailsEnabled = false;
    private const string DefaultLocale = SupportedLocales.English;

    public EfCoreNotificationsSettingsRepository(NotificationsContext context) : base(context)
    {
    }

    public async Task<NotificationsSettings> GetByIdOrDefaultAsync(Guid id,
        bool asNoTracking = false, CancellationToken ct = default)
    {
        var settings = await base.GetByIdAsync(id, asNoTracking: asNoTracking, ct: ct);
        if (settings == null)
        {
            settings = BuildDefaultSettings(id);
        }

        return settings;
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

    public async Task<IReadOnlyDictionary<Guid, NotificationsSettings>> GetSettingsByIds(Guid[] ids,
        CancellationToken ct = default)
    {
        var idSet = ids.ToHashSet(); // small performance optimization
        var foundDict = await Context.NotificationsSettings
            .Where(s => idSet.Contains(s.UserId))
            .ToDictionaryAsync(
                s => s.UserId,
                s => s,
                cancellationToken: ct
            );

        var result = new Dictionary<Guid, NotificationsSettings>(ids.Length);
        foreach (var id in ids)
        {
            result[id] = foundDict.TryGetValue(id, out var settings) ? settings : BuildDefaultSettings(id);
        }

        return result;
    }

    private static NotificationsSettings BuildDefaultSettings(Guid userId)
    {
        return new NotificationsSettings()
        {
            UserId = userId,
            Locale = DefaultLocale,
            PushesEnabled = DefaultPushesEnabled,
            EmailsEnabled = DefaultEmailsEnabled
        };
    }
}