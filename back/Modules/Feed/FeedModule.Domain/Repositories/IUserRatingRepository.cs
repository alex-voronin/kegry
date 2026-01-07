using FeedModule.Domain.Aggregates;
using SharedCore.Interfaces;

namespace FeedModule.Domain.Repositories;

public interface IUserRatingRepository : IRepository<UserRating>
{
    public Task UpsertRatingAtomic(
        Guid userId,
        decimal? oldRating,
        decimal newRating,
        CancellationToken ct = default);
}