using ChatModule.Domain.Entities;
using SharedCore.Interfaces;

namespace ChatModule.Domain.Repositories;

public interface IActiveChatUserLinkRepository : IRepository<ActiveChatUserLink>
{
    public Task<IReadOnlyList<Guid[]>> BatchDeleteLinksToDeletedChats(int limit, CancellationToken ct = default);
}