using FeedModule.Domain.Aggregates;
using FeedModule.Domain.Aggregates.ReadOnly;
using FeedModule.Domain.Services;
using NetTopologySuite.Geometries;

namespace FeedModule.Domain.Repositories;

public interface IFeedCandidateRepository
{
    public Task<FeedCandidate[]> GetFeedCandidatesInRadius(
        UserProfile userProfile,
        Point userCoordinates,
        Filter filter,
        int skippedUserCutOffInDays,
        int limit,
        CancellationToken ct);
}