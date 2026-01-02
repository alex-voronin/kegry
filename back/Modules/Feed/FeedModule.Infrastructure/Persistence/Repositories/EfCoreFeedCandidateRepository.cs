using FeedModule.Domain.Aggregates;
using FeedModule.Domain.Aggregates.ReadOnly;
using FeedModule.Domain.Repositories;
using FeedModule.Domain.Services;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using SharedCore.Services.LifeTimes;

namespace FeedModule.Infrastructure.Persistence.Repositories;

internal class EfCoreFeedCandidateRepository : IFeedCandidateRepository, IScoped 
{
    private readonly FeedContext _context;
    private TimeProvider _timeProvider;

    public EfCoreFeedCandidateRepository(FeedContext context, TimeProvider timeProvider)
    {
        _context = context;
        _timeProvider = timeProvider;
    }

    public async Task<FeedCandidate[]> GetFeedCandidatesInRadius(
        UserProfile userProfile,
        Point userCoordinates,
        Filter filter,
        int skippedUserCutOffInDays,
        int limit,
        CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(_timeProvider.GetUtcNow().DateTime);
        var now = _timeProvider.GetUtcNow();
        var skipCutoff = now.AddDays(skippedUserCutOffInDays);
        
        var query = _context.UserProfiles.AsNoTracking()
            .Join(
                _context.Locations.AsNoTracking(),
                u => u.Id,
                l => l.UserId,
                (u, l) => new { u, l }
            )
            .LeftJoin(
                _context.FeedProfiles.AsNoTracking()
                    .Where(fp => fp.MyUserId == userProfile.Id),
                ul => ul.u.Id,
                fp => fp.OtherUserId,
                (ul, fp) => new
                {
                    User = ul.u, 
                    Location = ul.l, 
                    FeedProfile = fp // can be null
                }
            )
            .Where(tuple =>
                EF.Functions.IsWithinDistance(
                    geometry: tuple.Location.Coordinates,
                    anotherGeometry: userCoordinates,
                    distance: filter.RadiusMeters,
                    useSpheroid: false
                )
            )
            .Where(tuple => tuple.User.Id != userProfile.Id)
            .Where(tuple => tuple.FeedProfile == null || !tuple.FeedProfile.IsBlocked)
            .Where(tuple => tuple.FeedProfile == null || !tuple.FeedProfile.IsInMeeting)
            .Where(tuple => tuple.FeedProfile == null || tuple.FeedProfile.SkippedAt < skipCutoff)
            .Where(tuple => tuple.FeedProfile == null || tuple.FeedProfile.MeetingTimeExpiresAt <= now);

        if (filter.Gender.HasValue)
        {
            query = query.Where(x => x.User.Gender == filter.Gender.Value);
        }

        if (filter.AgeFrom.HasValue)
        {
            var minBirthDate = today.AddYears(-filter.AgeFrom.Value);
            query = query.Where(x => x.User.Birthday <= minBirthDate);
        }

        if (filter.AgeTo.HasValue)
        {
            var maxBirthDate = today.AddYears(-(filter.AgeTo.Value + 1)).AddDays(1);
            query = query.Where(x => x.User.Birthday >= maxBirthDate);
        }

        if (filter.HobbyIds.Length > 0)
        {
            query = query.Where(x => x.User.Hobbies.Any(h => filter.HobbyIds.Contains(h.Id)));
        }

        var resultsQuery = query
            .Select(tuple => new
            {
                tuple.User,
                Rating = tuple.FeedProfile != null ? tuple.FeedProfile.Rating : (decimal?)null,
                DistanceMeters =
                    PostgisFunctions.DistanceSphere(
                        tuple.Location.Coordinates,
                        userCoordinates
                    ),
                LastSeenAt = tuple.FeedProfile != null ? tuple.FeedProfile.LastSeenAt : (DateTimeOffset?)null
            })
            .OrderBy(candidate => candidate.LastSeenAt.HasValue ? 1 : 0)
            .ThenBy(candidate => candidate.DistanceMeters);

        return await resultsQuery
            .Take(limit)
            .Select(candidate => new FeedCandidate()
            {
                User = candidate.User,
                Rating = candidate.Rating,
                DistanceMeters = (int)candidate.DistanceMeters
            })
            .ToArrayAsync(ct);
    }
}