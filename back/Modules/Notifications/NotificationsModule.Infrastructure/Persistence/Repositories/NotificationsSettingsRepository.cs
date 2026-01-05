using NotificationModule.Domain.Aggregates;
using Shared.Infrastructure.Persistence;

namespace NotificationsModule.Infrastructure.Persistence.Repositories;

internal class NotificationsSettingsRepository :
    Repository<NotificationsContext, NotificationsSettings>,
    INotificationsSettingsRepository
{
    public NotificationsSettingsRepository(NotificationsContext context) : base(context)
    {
    }
}