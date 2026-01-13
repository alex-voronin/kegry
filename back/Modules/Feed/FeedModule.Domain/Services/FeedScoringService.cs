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

    public FeedScoringService(
        IFeedCandidateRepository feedCandidateRepository,
        ILocationRepository locationRepository,
        IUserRepository userRepository,
        IFilterRepository filterRepository)
    {
        _feedCandidateRepository = feedCandidateRepository;
        _locationRepository = locationRepository;
        _userRepository = userRepository;
        _filterRepository = filterRepository;
    }

    public async Task<FeedCandidate[]> GetFeedAsync(
        Guid userId,
        int limit = 30,
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
            limit: FeedScoringParameters.UserLimitPerDatabaseQuery,
            skippedUserCutOffInDays: FeedScoringParameters.SkippedUserCutOffInDays,
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
        return candidates
            .Select(c => new
            {
                Candidate = c,
                Score = CalculateScore(user, c, filter)
            })
            .OrderByDescending(x => x.Score)
            .Take(limit)
            .Select(candidateAndScore => candidateAndScore.Candidate)
            .ToArray();
    }

    internal static double CalculateScore(User user, FeedCandidate candidate, Filter filter)
    {
        // distance scoring
        var distanceScore = MathFunctions.Gaussian(
            value: candidate.DistanceMeters,
            origin: 0,
            standardDeviation: filter.RadiusMeters / 2.0);

        // hobbies scoring
        var hobbiesScore = 0.0;
        if (user.Hobbies.Count > 0)
        {
            var hobbyIds = user.Hobbies.Select(hobby => hobby.Id).ToHashSet();
            var matches = candidate.User.Hobbies
                .Count(h => hobbyIds.Contains(h.Id));

            hobbiesScore = (double)matches / hobbyIds.Count;
        }

        // age scoring
        var daysDifference = Math.Abs(user.Birthday.ToDateTime(TimeOnly.MinValue).Ticks -
                                      candidate.User.Birthday.ToDateTime(TimeOnly.MinValue).Ticks) / TimeSpan.TicksPerDay;
        var ageScore = MathFunctions.Gaussian(
            value: daysDifference / 365.0,
            origin: 0,
            standardDeviation: 6.0);

        // rating scoring
        var ratingScore = (candidate.Rating.HasValue ? (double)candidate.Rating : 4.0) / 5.0;

        //TODO: add inactivity time to the scoring calculation formula

        return FeedScoringParameters.DistanceWeightInScore * distanceScore +
               FeedScoringParameters.HobbiesWeightInScore * hobbiesScore +
               FeedScoringParameters.AgeWeightInScore * ageScore +
               FeedScoringParameters.RatingWeightInScore * ratingScore;
    }
}