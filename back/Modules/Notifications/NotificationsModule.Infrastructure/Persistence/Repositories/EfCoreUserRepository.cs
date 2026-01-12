using NotificationModule.Domain.Aggregates;
using NotificationModule.Domain.Repositories;
using Shared.Infrastructure.Persistence;

namespace NotificationsModule.Infrastructure.Persistence.Repositories;

internal class EfCoreUserRepository : Repository<NotificationsContext, User>, IUserRepository
{
    public EfCoreUserRepository(NotificationsContext context) : base(context)
    {
    }
}
