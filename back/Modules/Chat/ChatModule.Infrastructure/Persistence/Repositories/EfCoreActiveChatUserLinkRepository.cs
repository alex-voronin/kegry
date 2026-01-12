using ChatModule.Domain.Aggregates;
using ChatModule.Domain.Repositories;
using Shared.Infrastructure.Persistence;

namespace ChatModule.Infrastructure.Persistence.Repositories;

public class EfCoreActiveChatUserLinkRepository : Repository<ChatContext, ActiveChatUserLink>,
    IActiveChatUserLinkRepository
{
    public EfCoreActiveChatUserLinkRepository(ChatContext context) : base(context)
    {
    }
}