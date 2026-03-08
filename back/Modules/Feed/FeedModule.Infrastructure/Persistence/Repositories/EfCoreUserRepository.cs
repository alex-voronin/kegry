using FeedModule.Domain.Aggregates.ReadOnly;
using FeedModule.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Persistence;

namespace FeedModule.Infrastructure.Persistence.Repositories;

internal class EfCoreUserRepository : Repository<FeedContext, User>, IUserRepository
{
    public EfCoreUserRepository(FeedContext context) : base(context)
    {
    }

    public async Task<User[]> GetBlockedUsersAsync(Guid userId, int skip, int limit, CancellationToken ct = default)
    {
        var users = await Context.FeedUserRelations
            .AsNoTracking()
            .Where(r => r.MyUserId == userId && r.IsBlocked)
            .OrderByDescending(r => r.LastSeenAt)
            .Skip(skip)
            .Take(limit)
            .Join(Context.Users,
                relation => relation.OtherUserId,
                user => user.Id,
                (relation, user) => user)
            .ToArrayAsync(ct);

        return users;
    }

    public Task<int> GetBlockedUsersCount(Guid userId, CancellationToken ct = default)
    {
        return Context.FeedUserRelations
            .CountAsync(r => r.MyUserId == userId && r.IsBlocked, ct);
    }

    public async Task<User[]> GetFriendsAsync(Guid userId, int skip, int limit, CancellationToken ct = default)
    {
        var users = await FriendsQuery(userId)
            .OrderBy(u => u.FirstName)
            .ThenBy(u => u.LastName)
            .Skip(skip)
            .Take(limit)
            .ToArrayAsync(ct);

        return users;
    }

    public Task<int> GetFriendsCount(Guid userId, CancellationToken ct = default)
    {
        return FriendsQuery(userId).CountAsync(ct);
    }

    private IQueryable<User> FriendsQuery(Guid userId)
    {
        return Context.FeedUserRelations
            .AsNoTracking()
            .Where(r => r.MyUserId == userId && r.IsFriend && !r.IsBlocked)
            .Join(Context.Users,
                relation => relation.OtherUserId,
                user => user.Id,
                (relation, user) => user);
    }
}