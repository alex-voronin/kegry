using ChatModule.Domain.Aggregates;
using ChatModule.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Persistence;
using Wolverine.EntityFrameworkCore;

namespace ChatModule.Infrastructure.Persistence.Repositories;

public class EfCoreChatRepository : RepositoryOutbox<ChatContext, Chat>, IChatRepository
{
    public EfCoreChatRepository(IDbContextOutbox<ChatContext> contextOutbox) : base(contextOutbox)
    {
    }

    public Task<Chat?> GetActiveChatByUserId(Guid userId, bool loadMessages = true, CancellationToken ct = default)
    {
        IQueryable<Chat> query = Context.Chats
            .Where(c => c.UserLinks.Any(l => l.UserId == userId))
            .Include(c => c.UserLinks)
            .ThenInclude(ul => ul.User);
        if (loadMessages)
        {
            query = query.Include(c => c.Messages);
        }

        return query.FirstOrDefaultAsync(ct);
    }

    public Task<int> GetMessageCountAsync(Guid chatId, Guid userId, CancellationToken ct = default)
    {
        return Context.Messages
            .CountAsync(m => m.ChatId == chatId && m.FromUserId == userId, ct);
    }

    public async Task MarkExpiredChatsAsDeleted(CancellationToken ct = default)
    {
        await Context.Chats
            .Where(c => c.ExpiresAt < DateTimeOffset.UtcNow)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(c => c.IsDeleted, true), ct);
    }
}