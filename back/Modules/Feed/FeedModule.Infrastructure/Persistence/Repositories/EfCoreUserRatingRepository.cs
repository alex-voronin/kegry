using FeedModule.Domain.Aggregates;
using FeedModule.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Persistence;

namespace FeedModule.Infrastructure.Persistence.Repositories;

public class EfCoreUserRatingRepository : Repository<FeedContext, UserRating>, IUserRatingRepository
{
    public EfCoreUserRatingRepository(FeedContext context) : base(context)
    {
    }

    public async Task UpsertRatingAtomic(
        Guid userId,
        decimal oldRating,
        decimal newRating,
        CancellationToken ct = default)
    {
        var deltaRating = newRating - oldRating;
        var deltaReviewersCount = oldRating > 0m ? 0 : 1;

        await Context.Database.ExecuteSqlInterpolatedAsync(
            $"""
                INSERT INTO feed.user_ratings (user_id, rating, reviewers_count)
                VALUES ({userId}, {newRating}, {deltaReviewersCount})
                ON CONFLICT (user_id)
                DO UPDATE SET
                    rating = (feed.user_ratings.rating * feed.user_ratings.reviewers_count + {deltaRating}) /
                             (feed.user_ratings.reviewers_count + {deltaReviewersCount}),
                    reviewers_count = feed.user_ratings.reviewers_count + {deltaReviewersCount}
            """, ct);
    }
}