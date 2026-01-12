using ChatModule.Domain.Aggregates;
using ChatModule.Domain.Repositories;
using Shared.Infrastructure.Persistence;

namespace ChatModule.Infrastructure.Persistence.Repositories;

public class EfCoreUserRepository : Repository<ChatContext, User>, IUserRepository
{
    public EfCoreUserRepository(ChatContext context) : base(context)
    {
    }
}
