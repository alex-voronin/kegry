using ChatModule.Domain.Aggregates;
using ChatModule.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Persistence;

namespace ChatModule.Infrastructure.Persistence.Repositories;

public class ChatRepository : Repository<ChatContext, Chat>, IChatRepository
{
    public ChatRepository(ChatContext context) : base(context)
    {
    }

    public Task<Chat?> GetActiveChatByUserId(Guid userId, CancellationToken ct = default)
    {
        return Context.ActiveChatUserLinks
            .Where(l => l.UserId == userId)
            .Select(l => l.Chat)
            .Include(c => c.UserLinks)
            .ThenInclude(u => u.User)
            .Include(c => c.Messages)
            .FirstOrDefaultAsync(ct);
    }
}