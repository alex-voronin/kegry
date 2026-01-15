using ChatModule.Domain.Aggregates;
using SharedCore.Interfaces;

namespace ChatModule.Domain.Repositories;

public interface IChatRepository : IRepositoryOutbox<Chat>
{
    Task<Chat?> GetActiveChatByUserId(Guid userId, bool loadMessages = true, CancellationToken ct = default);
    Task<int> GetMessageCountAsync(Guid chatId, Guid userId, CancellationToken ct = default);
    Task MarkExpiredChatsAsDeleted(CancellationToken ct = default);
}
