using ChatModule.Domain.Aggregates;
using ChatModule.Domain.Repositories;
using Shared.Infrastructure.Persistence;

namespace ChatModule.Infrastructure.Persistence.Repositories;

public class ActiveChatUserLinkRepository : Repository<ChatContext, ActiveChatUserLink>,
    IActiveChatUserLinkRepository
{
    public ActiveChatUserLinkRepository(ChatContext context) : base(context)
    {
    }
}