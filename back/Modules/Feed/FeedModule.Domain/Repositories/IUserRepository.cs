using FeedModule.Domain.Aggregates.ReadOnly;
using SharedCore.Interfaces;

namespace FeedModule.Domain.Repositories;

public interface IUserRepository : IRepository<User>
{
    public Task<User[]> GetBlockedUsersAsync(
        Guid userId,
        int skip,
        int limit,
        CancellationToken ct = default);
}