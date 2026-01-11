using ChatModule.Domain.Aggregates;
using ChatModule.Domain.Repositories;
using Shared.Infrastructure.Persistence;

namespace ChatModule.Infrastructure.Persistence.Repositories;

public class UserRepository : Repository<ChatContext, User>, IUserRepository
{
    public UserRepository(ChatContext context) : base(context)
    {
    }
}
