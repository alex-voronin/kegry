using ChatModule.Domain.Aggregates;
using SharedCore.Interfaces;

namespace ChatModule.Domain.Repositories;

public interface IChatRepository : IRepositoryOutbox<Chat>
{
    Task<Chat?> GetActiveChatByUserId(Guid userId, CancellationToken ct = default);
    Task MarkExpiredChatsAsDeleted(CancellationToken ct = default);
}
