using FeedModule.Domain.Aggregates;
using FeedModule.Domain.Aggregates.ReadOnly;
using FeedModule.Domain.Exceptions;
using FeedModule.Domain.Repositories;
using SharedCore.Math;
using SharedCore.Services.LifeTimes;

namespace FeedModule.Domain.Services;

internal sealed class FeedScoringService : IFeedScoringService, IScoped
{
    private readonly IFeedCandidateRepository _feedCandidateRepository;
    private readonly ILocationRepository _locationRepository;
    private readonly IUserRepository _userRepository;
    private readonly IFilterRepository _filterRepository;
    private readonly TimeProvider _timeProvider;

    public FeedScoringService(
        IFeedCandidateRepository feedCandidateRepository,
        ILocationRepository locationRepository,
        IUserRepository userRepository,
        IFilterRepository filterRepository,
        TimeProvider timeProvider)
    {
        _feedCandidateRepository = feedCandidateRepository;
        _locationRepository = locationRepository;
        _userRepository = userRepository;
        _filterRepository = filterRepository;
        _timeProvider = timeProvider;
    }

    public async Task<FeedCandidate[]> GetFeedAsync(
        Guid userId,
        int limit = FeedParameters.FeedLength,
        CancellationToken ct = default)
    {
        var filter = await _filterRepository.GetByIdAsync(userId, asNoTracking: true, ct: ct);
        filter ??= Filter.CreateDefault(userId);
        var userLocation = await _locationRepository.GetByIdAsync(userId, asNoTracking: true, ct: ct);
        if (userLocation == null)
        {
            throw FeedExceptions.UserLocationIsUnknown(userId);
        }

        var userProfile = await _userRepository.GetByIdAsync(userId,
            asNoTracking: true,
            includes: [u => u.Hobbies],
            ct: ct);
        if (userProfile == null)
        {
            throw FeedExceptions.UserProfileIsNotFound(userId);
        }

        var candidates = await _feedCandidateRepository.GetFeedCandidatesInRadius(
            user: userProfile,
            userCoordinates: userLocation.Coordinates,
            limit: FeedParameters.UserLimitPerDatabaseQuery,
            skippedUserCutOffInDays: FeedParameters.SkippedUserCutOffInDays,
            filter: filter,
            ct: ct);

        return RankCandidates(userProfile, candidates, filter, limit);
    }

    private FeedCandidate[] RankCandidates(
        User user,
        FeedCandidate[] candidates,
        Filter filter,
        int limit)
    {
        var ratingCutOffDateTime = _timeProvider.GetUtcNow() -
                                   TimeSpan.FromHours(FeedParameters.IgnoredUserRatingDowngradeInHours);

        var tailCandidates = candidates
            .Where(c => c.LastSeenAt != null && c.LastSeenAt > ratingCutOffDateTime)
            .OrderBy(c => c.LastSeenAt);

        var headCandidates = candidates
            .Where(c => c.LastSeenAt == null || c.LastSeenAt <= ratingCutOffDateTime)
            .Select(c => new
            {
                Candidate = c,
                Score = CalculateScore(user, c, filter)
            })
            .OrderByDescending(t => t.Score);

        return headCandidates
            .Select(x => x.Candidate)
            .Concat(tailCandidates)
            .Take(limit)
            .ToArray();
    }

    internal static double CalculateScore(User user, FeedCandidate candidate, Filter filter)
    {
        // distance scoring
        var distanceScore = MathFunctions.Gaussian(
            value: candidate.DistanceMeters,
            origin: 0,
            standardDeviation: filter.RadiusMeters / 2.0);

        var hobbyCategoriesScore = 0.0;
        var hobbiesScore = 0.0;
        if (user.Hobbies.Count > 0)
        {
            // hobbies scoring
            var hobbyIds = user.Hobbies.Select(hobby => hobby.Id).ToHashSet();
            var hobbyMatches = candidate.User.Hobbies
                .Count(h => hobbyIds.Contains(h.Id));
            hobbiesScore = (double)hobbyMatches / hobbyIds.Count;

            // hobby categories scoring
            var hobbyCategoryIds = user.Hobbies.Select(hobby => hobby.CategoryId).ToHashSet();
            var categoriesMatches = candidate.User.Hobbies
                .Select(hobby => hobby.CategoryId)
                .Distinct()
                .Count(candidateCategoryId => hobbyCategoryIds.Contains(candidateCategoryId));
            hobbyCategoriesScore = (double)categoriesMatches / hobbyCategoryIds.Count;
        }

        // age scoring
        var daysDifference = Math.Abs(user.Birthday.ToDateTime(TimeOnly.MinValue).Ticks -
                                      candidate.User.Birthday.ToDateTime(TimeOnly.MinValue).Ticks) /
                             TimeSpan.TicksPerDay;
        var ageScore = MathFunctions.Gaussian(
            value: daysDifference / 365.0,
            origin: 0,
            standardDeviation: 6.0);

        // rating scoring
        var ratingScore = (candidate.Rating.HasValue ? (double)candidate.Rating : 4.0) / 5.0;

        //TODO: add inactivity time to the scoring calculation formula

        return FeedParameters.DistanceWeightInScore * distanceScore +
               FeedParameters.HobbiesWeightInScore * hobbiesScore +
               FeedParameters.HobbyCategoriesWeightInScore * hobbyCategoriesScore +
               FeedParameters.AgeWeightInScore * ageScore +
               FeedParameters.RatingWeightInScore * ratingScore;
    }
}