using NotificationModule.Domain.Aggregates;
using NotificationModule.Domain.Repositories;
using Shared.Infrastructure.Persistence;

namespace NotificationsModule.Infrastructure.Persistence.Repositories;

internal class UserRepository : Repository<NotificationsContext, User>, IUserRepository
{
    public UserRepository(NotificationsContext context) : base(context)
    {
    }
}
