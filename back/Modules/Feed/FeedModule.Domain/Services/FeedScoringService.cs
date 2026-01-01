using FeedModule.Domain.Aggregates;
using FeedModule.Domain.Aggregates.ReadOnly;
using FeedModule.Domain.Exceptions;
using FeedModule.Domain.Repositories;
using SharedCore.Services.LifeTimes;

namespace FeedModule.Domain.Services;

public sealed class FeedScoringService : IFeedScoringService, ISingleton
{
    private readonly IFeedCandidateRepository _feedCandidateRepository;
    private readonly ILocationRepository _locationRepository;
    private readonly IUserProfileRepository _userProfileRepository;
    private readonly IFilterRepository _filterRepository;

    internal FeedScoringService(
        IFeedCandidateRepository feedCandidateRepository,
        ILocationRepository locationRepository,
        IUserProfileRepository userProfileRepository,
        IFilterRepository filterRepository)
    {
        _feedCandidateRepository = feedCandidateRepository;
        _locationRepository = locationRepository;
        _userProfileRepository = userProfileRepository;
        _filterRepository = filterRepository;
    }

    public async Task<FeedCandidate[]> GetFeedAsync(
        Guid userId,
        CancellationToken ct)
    {
        var filter = await _filterRepository.GetByIdAsync(userId, asNoTracking:true, ct: ct); 
        filter ??= Filter.CreateDefault(userId);
        var userLocation = await _locationRepository.GetByIdAsync(userId, asNoTracking: true, ct: ct);
        if (userLocation == null)
        {
            throw FeedExceptions.UserLocationIsUnknown(userId);
        }

        var userProfile = await _userProfileRepository.GetByIdAsync(userId,
            asNoTracking: true,
            includes: [u => u.Hobbies],
            ct: ct);
        if (userProfile == null)
        {
            throw FeedExceptions.UserProfileIsNotFound(userId);
        }
        
        var candidates = await _feedCandidateRepository.GetFeedCandidatesInRadius(
            userProfile: userProfile,
            userCoordinates: userLocation.Coordinates,
            limit: FeedScoringParameters.UserLimitPerDatabaseQuery, 
            skippedUserCutOffInDays: FeedScoringParameters.SkippedUserCutOffInDays,
            filter: filter,
            ct: ct);

        return RankCandidates(userProfile, candidates);
    }
    
    private FeedCandidate[] RankCandidates(
        UserProfile userProfile,
        FeedCandidate[] candidates)
    {
        return candidates
            .Select(c => new
            {
                Candidate = c,
                Score = CalculateScore(userProfile, c)
            })
            .OrderByDescending(x => x.Score)
            .Select(candidateAndScore => candidateAndScore.Candidate)
            .ToArray();
    }
    private static double CalculateScore(UserProfile userProfile, FeedCandidate candidate)
    {
        // distance scoring
        var distanceScore = 1.0 / (1.0 + candidate.DistanceMeters);
        
        // hobbies scoring
        var hobbiesScore = 0.0;
        if (userProfile.Hobbies.Count > 0)
        {
            var hobbyIds  = userProfile.Hobbies.Select(hobby => hobby.Id).ToHashSet();
            var matches = candidate.User.Hobbies
                .Count(h => hobbyIds.Contains(h.Id));

            hobbiesScore = (double)matches / hobbyIds.Count;
        }

        // age scoring
        var daysDifference = Math.Abs(userProfile.Birthday.ToDateTime(TimeOnly.MinValue).Ticks - 
                                      candidate.User.Birthday.ToDateTime(TimeOnly.MinValue).Ticks) / TimeSpan.TicksPerDay;
        var ageScore = 1.0 / (1.0 + (daysDifference / 365.0));

        return FeedScoringParameters.DistanceWeightInScoring * distanceScore + 
               FeedScoringParameters.HobbiesWeightInScoring * hobbiesScore +
               FeedScoringParameters.AgeWeightInScoring * ageScore;
    }
}