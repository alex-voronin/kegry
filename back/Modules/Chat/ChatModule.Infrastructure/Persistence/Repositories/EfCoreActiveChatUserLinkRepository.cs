using System.Runtime.InteropServices;
using ChatModule.Domain.Entities;
using ChatModule.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Persistence;

namespace ChatModule.Infrastructure.Persistence.Repositories;

public class EfCoreActiveChatUserLinkRepository : Repository<ChatContext, ActiveChatUserLink>,
    IActiveChatUserLinkRepository
{
    public EfCoreActiveChatUserLinkRepository(ChatContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<Guid[]>> BatchDeleteLinksToDeletedChats(int limit = 100, CancellationToken ct = default)
    {
        var linksToDelete = await Context.ActiveChatUserLinks
            .Where(l => l.Chat.IsDeleted)
            .OrderBy(l => l.ChatId)
            .Take(limit)
            .Select(l => new { l.ChatId, l.UserId })
            .ToListAsync(ct);

        var chatIds = linksToDelete.Select(l => l.ChatId);

        await Context.ActiveChatUserLinks
            .Where(l => chatIds.Contains(l.ChatId))
            .ExecuteDeleteAsync(ct);

        return linksToDelete.GroupBy(l => l.ChatId)
            .Select(g => g.Select(l => l.UserId).ToArray()).ToArray();
    }
}