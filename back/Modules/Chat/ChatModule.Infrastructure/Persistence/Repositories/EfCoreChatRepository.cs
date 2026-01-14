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

    public Task<Chat?> GetActiveChatByUserId(Guid userId, CancellationToken ct = default)
    {
        return Context.Chats
            .Where(c => c.UserLinks.Any(l => l.UserId == userId))
            .Include(c => c.UserLinks)
            .ThenInclude(ul => ul.User)
            .Include(c => c.Messages)
            .FirstOrDefaultAsync(ct);
    }

    public async Task MarkExpiredChatsAsDeleted(CancellationToken ct = default)
    {
        await Context.Chats
            .Where(c => c.ExpiresAt < DateTimeOffset.UtcNow)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(c => c.IsDeleted, true), ct);
    }
}