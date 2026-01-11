using ChatModule.Domain.Aggregates;
using SharedCore.Interfaces;

namespace ChatModule.Domain.Repositories;

public interface IChatRepository : IRepository<Chat>
{
    Task<Chat?> GetActiveChatByUserId(Guid userId, CancellationToken ct = default);
}
