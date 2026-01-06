using System.Linq.Expressions;
using AutoFixture.Xunit2;
using FeedModule.Domain.Aggregates;
using FeedModule.Domain.Aggregates.ReadOnly;
using FeedModule.Domain.Repositories;
using FeedModule.Domain.Services;
using FeedModule.Domain.ValueObjects;
using NetTopologySuite.Geometries;
using NSubstitute;
using SharedCore.Exceptions;
using Shouldly;
using Tests.Shared.AutoFixture;
using Hobby = FeedModule.Domain.Aggregates.ReadOnly.Hobby;
using Location = FeedModule.Domain.Aggregates.ReadOnly.Location;

namespace Tests.FeedModule.Domain;

public class FeedScoringServiceTests
{
    [Theory, AutoNSubstituteData]
    internal async Task GetFeedAsync_WhenUserLocationIsMissing_ThenThrow(
        [Frozen] ILocationRepository locationRepository,
        FeedScoringService service,
        Guid userId)
    {
        locationRepository.GetByIdAsync(userId, asNoTracking: true).Returns((Location?)null);

        var exception =
            await Should.ThrowAsync<HttpCodeException>(() => service.GetFeedAsync(userId));
        exception.ErrorCode.ShouldBe(ErrorCode.NotFound);
    }

    [Theory, AutoNSubstituteData]
    internal async Task GetFeedAsync_WhenUserProfileIsMissing_ThenThrow(
        [Frozen] ILocationRepository locationRepository,
        [Frozen] IUserProfileRepository userProfileRepository,
        FeedScoringService service,
        Guid userId)
    {
        SetupRepositoryData(locationRepository, userId);
        userProfileRepository.GetByIdAsync(userId, asNoTracking: true,
                includes: Arg.Any<Expression<Func<UserProfile, object?>>[]>())
            .Returns((UserProfile?)null);

        var exception =
            await Should.ThrowAsync<HttpCodeException>(() => service.GetFeedAsync(userId));
        exception.ErrorCode.ShouldBe(ErrorCode.NotFound);
    }

    [Theory, AutoNSubstituteData]
    internal async Task GetFeedAsync_WhenFilterIsMissing_ThenUseDefaultFilter(
        [Frozen] ILocationRepository locationRepository,
        [Frozen] IUserProfileRepository userProfileRepository,
        [Frozen] IFilterRepository filterRepository,
        [Frozen] IFeedCandidateRepository feedCandidateRepository,
        FeedScoringService service,
        Guid userId)
    {
        var defaultFilter = Filter.CreateDefault(userId);
        var location = SetupRepositoryData(locationRepository, userId);
        var userProfile = SetupRepositoryData(userProfileRepository, userId);
        filterRepository.GetByIdAsync(userId, asNoTracking: true).Returns((Filter?)null);

        await service.GetFeedAsync(userId);
        await feedCandidateRepository.Received(1).GetFeedCandidatesInRadius(
            userProfile,
            location.Coordinates,
            defaultFilter,
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Any<CancellationToken>());
    }

    [Theory, AutoNSubstituteData]
    internal async Task GetFeedAsync_WhenFilterExists_ThenUseIt(
        [Frozen] ILocationRepository locationRepository,
        [Frozen] IUserProfileRepository userProfileRepository,
        [Frozen] IFilterRepository filterRepository,
        [Frozen] IFeedCandidateRepository feedCandidateRepository,
        FeedScoringService service,
        Guid userId)
    {
        SetupRepositoryData(locationRepository, userId);
        SetupRepositoryData(userProfileRepository, userId);
        var filter = new Filter
        {
            UserId = userId,
            RadiusMeters = 5000,
            MeetingDeadline = MeetingDeadline.Now,
            AgeFrom = null,
            AgeTo = null,
            Gender = null,
            HobbyIds = []
        };
        filterRepository.GetByIdAsync(userId, asNoTracking: true)
            .Returns(filter);

        await service.GetFeedAsync(userId);
        await feedCandidateRepository.Received(1)
            .GetFeedCandidatesInRadius(
                Arg.Any<UserProfile>(),
                Arg.Any<Point>(),
                filter,
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>());
    }

    [Theory, AutoNSubstituteData]
    internal async Task GetFeedAsync_WhenNoCandidatesFound_ThenReturnsEmptyResult(
        [Frozen] ILocationRepository locationRepository,
        [Frozen] IUserProfileRepository userProfileRepository,
        [Frozen] IFeedCandidateRepository feedCandidateRepository,
        FeedScoringService service,
        Guid userId)
    {
        SetupRepositoryData(locationRepository, userId);
        SetupRepositoryData(userProfileRepository, userId);

        feedCandidateRepository.GetFeedCandidatesInRadius(
                Arg.Any<UserProfile>(),
                Arg.Any<Point>(),
                Arg.Any<Filter>(),
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns([]);

        var result = await service.GetFeedAsync(userId);
        result.ShouldBeEmpty();
    }

    [Theory, AutoNSubstituteData]
    internal async Task GetFeedAsync_WhenCandidatesFound_ThenRankByScore(
        [Frozen] ILocationRepository locationRepository,
        [Frozen] IUserProfileRepository userProfileRepository,
        [Frozen] IFeedCandidateRepository feedCandidateRepository,
        FeedScoringService service,
        Guid userId)
    {
        SetupRepositoryData(locationRepository, userId);
        var userProfile = SetupRepositoryData(userProfileRepository, userId);

        var candidate1 = new FeedCandidate
        {
            DistanceMeters = 100,
            Rating = 5,
            User = new UserProfile
            {
                Id = Guid.NewGuid(),
                Birthday = new DateOnly(1990, 1, 1),
                Hobbies = userProfile.Hobbies
            }
        };

        var candidate2 = new FeedCandidate
        {
            DistanceMeters = 10000,
            Rating = 1,
            User = new UserProfile
            {
                Id = Guid.NewGuid(),
                Birthday = new DateOnly(2000, 1, 1),
                Hobbies = []
            }
        };

        feedCandidateRepository.GetFeedCandidatesInRadius(
                Arg.Any<UserProfile>(),
                Arg.Any<Point>(),
                Arg.Any<Filter>(),
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns([candidate2, candidate1]);
        var result = await service.GetFeedAsync(userId);
        
        result.Length.ShouldBe(2);
        result[0].User.Id.ShouldBe(candidate1.User.Id);
        result[1].User.Id.ShouldBe(candidate2.User.Id);
    }

    [Theory, AutoNSubstituteData]
    internal async Task GetFeedAsync_WhenMoreCandidatesThanLimit_ThenReturnLimitedCount(
        [Frozen] ILocationRepository locationRepository,
        [Frozen] IUserProfileRepository userProfileRepository,
        [Frozen] IFeedCandidateRepository feedCandidateRepository,
        FeedScoringService service,
        Guid userId)
    {
        var limit = 2;
        SetupRepositoryData(locationRepository, userId);
        SetupRepositoryData(userProfileRepository, userId);

        var candidates = Enumerable.Range(0, 3).Select(i => new FeedCandidate
        {
            DistanceMeters = 100 * i,
            Rating = 5,
            User = new UserProfile { Id = Guid.NewGuid(), Birthday = new DateOnly(1990, 1, 1), Hobbies = [] }
        }).ToArray();
        feedCandidateRepository.GetFeedCandidatesInRadius(
                Arg.Any<UserProfile>(),
                Arg.Any<Point>(),
                Arg.Any<Filter>(),
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns(candidates);

        var result = await service.GetFeedAsync(userId, limit: limit);
        result.Length.ShouldBe(limit);
    }

    private Location SetupRepositoryData(ILocationRepository locationRepository, Guid userId)
    {
        var location = new Location { UserId = userId, Coordinates = new Point(0, 0) };
        locationRepository.GetByIdAsync(userId, asNoTracking: true).Returns(location);
        return location;
    }

    private UserProfile SetupRepositoryData(IUserProfileRepository userProfileRepository, Guid userId)
    {
        var userProfile = new UserProfile
        {
            Id = userId,
            Birthday = new DateOnly(1990, 1, 1),
            Hobbies = [new Hobby { Id = Guid.NewGuid(), Name = "Hiking" }]
        };
        userProfileRepository.GetByIdAsync(userId, 
                asNoTracking: true,
                includes: Arg.Any<Expression<Func<UserProfile, object?>>[]>())
            .Returns(userProfile);
        return userProfile;
    }
}