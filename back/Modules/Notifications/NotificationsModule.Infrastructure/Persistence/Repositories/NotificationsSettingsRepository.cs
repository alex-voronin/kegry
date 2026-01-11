using NotificationModule.Domain.Aggregates;
using NotificationModule.Domain.Repositories;
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