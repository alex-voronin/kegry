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
    private readonly TimeProvider _timeProvider;

    public EfCoreFeedCandidateRepository(FeedContext context, TimeProvider timeProvider)
    {
        _context = context;
        _timeProvider = timeProvider;
    }

    public async Task<FeedCandidate[]> GetFeedCandidatesInRadius(
        User user,
        Point userCoordinates,
        Filter filter,
        int skippedUserCutOffInDays,
        int limit,
        CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(_timeProvider.GetUtcNow().DateTime);
        var now = _timeProvider.GetUtcNow();
        var skipCutoff = now.AddDays(-skippedUserCutOffInDays);

        var query = _context.Users
            .Include(u => u.Hobbies)
            .AsNoTracking()
            .Join(
                _context.Locations.AsNoTracking(),
                u => u.Id,
                l => l.UserId,
                (u, l) => new { User = u, Location = l }
            )
            .Where(j =>
                EF.Functions.IsWithinDistance(
                    geometry: j.Location.Coordinates,
                    anotherGeometry: userCoordinates,
                    distance: filter.RadiusMeters,
                    useSpheroid: false
                )
            )
            .LeftJoin(
                _context.FeedUserRelations.AsNoTracking()
                    .Where(r => r.MyUserId == user.Id),
                j => j.User.Id,
                r => r.OtherUserId,
                (j, r) => new
                {
                    j.User,
                    j.Location,
                    FeedUserRelation = r
                }
            )
            .LeftJoin(
                _context.UserRatings.AsNoTracking(),
                j => j.User.Id,
                ur => ur.UserId,
                (j, ur) => new
                {
                    j.User,
                    j.Location,
                    j.FeedUserRelation,
                    UserRating = ur
                }
            )
            .Where(j => j.User.IsPublic)
            .Where(j => j.User.Id != user.Id)
            .Where(j => j.FeedUserRelation == null || !j.FeedUserRelation.IsBlocked)
            .Where(j => j.FeedUserRelation == null || !j.FeedUserRelation.IsInMeeting)
            .Where(j => j.FeedUserRelation == null || j.FeedUserRelation.SkippedAt < skipCutoff)
            .Where(j => j.FeedUserRelation == null || j.FeedUserRelation.InviteExpiresAt <= now);

        if (filter.Gender.HasValue)
        {
            query = query.Where(j => j.User.Gender == filter.Gender.Value);
        }

        if (filter.AgeFrom.HasValue)
        {
            var minBirthDate = today.AddYears(-filter.AgeFrom.Value);
            query = query.Where(j => j.User.Birthday <= minBirthDate);
        }

        if (filter.AgeTo.HasValue)
        {
            var maxBirthDate = today.AddYears(-(filter.AgeTo.Value + 1)).AddDays(1);
            query = query.Where(j => j.User.Birthday >= maxBirthDate);
        }

        if (filter.HobbyIds.Length > 0)
        {
            query = query.Where(j => j.User.Hobbies.Any(h => filter.HobbyIds.Contains(h.Id)));
        }

        var resultsQuery = query
            .Select(j => new
            {
                j.User,
                j.UserRating!.Rating,
                DistanceMeters =
                    EF.Functions.Distance(
                        geometry: j.Location.Coordinates,
                        anotherGeometry: userCoordinates,
                        useSpheroid: false),
                LastSeenAt = j.FeedUserRelation != null ? j.FeedUserRelation.LastSeenAt : (DateTimeOffset?)null
            })
            .OrderBy(candidate => candidate.LastSeenAt.HasValue ? 1 : 0)
            .ThenBy(candidate => candidate.DistanceMeters);

        return await resultsQuery
            .Take(limit)
            .Select(candidate => new FeedCandidate()
            {
                User = candidate.User,
                Rating = candidate.Rating,
                LastSeenAt = candidate.LastSeenAt,
                DistanceMeters = (int)candidate.DistanceMeters
            })
            .ToArrayAsync(ct);
    }
}