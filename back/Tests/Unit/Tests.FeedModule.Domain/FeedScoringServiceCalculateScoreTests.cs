using FeedModule.Domain.Aggregates;
using FeedModule.Domain.Aggregates.ReadOnly;
using FeedModule.Domain.Services;
using FeedModule.Domain.ValueObjects;
using Shouldly;
using Hobby = FeedModule.Domain.Aggregates.ReadOnly.Hobby;

namespace Tests.FeedModule.Domain;

public class FeedScoringServiceCalculateScoreTests
{
    private const double DistanceWeight = FeedParameters.DistanceWeightInScore;
    private const double HobbiesWeight = FeedParameters.HobbiesWeightInScore;
    private const double AgeWeight = FeedParameters.AgeWeightInScore;
    private const double RatingWeightInScore = FeedParameters.RatingWeightInScore;
    private const int FilterDistance = 30000;

    [Fact]
    public void CalculateScore_WhenPerfectMatch_ReturnsMaxScore()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var hobbyId = Guid.NewGuid();
        var userProfile = new User
        {
            Id = Guid.NewGuid(),
            Birthday = today,
            Hobbies = [new Hobby { Id = hobbyId, Name = "Test" }]
        };

        var candidate = new FeedCandidate
        {
            DistanceMeters = 0,
            Rating = 5,
            User = new User
            {
                Id = Guid.NewGuid(),
                Birthday = today,
                Hobbies = [new Hobby { Id = hobbyId, Name = "Test" }]
            }
        };

        var score = FeedScoringService.CalculateScore(
            userProfile,
            candidate,
            Filter.CreateDefault(userProfile.Id));

        var expectedScore = DistanceWeight + HobbiesWeight + AgeWeight + RatingWeightInScore;
        score.ShouldBe(expectedScore, tolerance: 0.0001);
    }

    [Fact]
    public void CalculateScore_WhenGoodMatch_ReturnsGoodScore()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var hobbyId = Guid.NewGuid();
        var userProfile = new User
        {
            Id = Guid.NewGuid(),
            Birthday = today,
            Hobbies = [new Hobby { Id = hobbyId, Name = "H1" }, new Hobby { Id = hobbyId, Name = "H2" }]
        };

        var candidate = new FeedCandidate
        {
            DistanceMeters = 15000,
            Rating = 5,
            User = new User
            {
                Id = Guid.NewGuid(),
                Birthday = userProfile.Birthday.AddYears(5),
                Hobbies = [new Hobby { Id = hobbyId, Name = "H1" }]
            }
        };

        var score = FeedScoringService.CalculateScore(
            userProfile,
            candidate,
            Filter.CreateDefault(userProfile.Id));

        score.ShouldBeGreaterThan(0.8);
    }

    [Fact]
    public void CalculateScore_WhenWorstMatch_ReturnsLowScore()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var userProfile = new User
        {
            Id = Guid.NewGuid(),
            Birthday = today,
            Hobbies = [new Hobby { Id = Guid.NewGuid(), Name = "Hobby1" }]
        };

        var candidate = new FeedCandidate
        {
            DistanceMeters = 100000,
            Rating = 0.5m,
            User = new User
            {
                Id = Guid.NewGuid(),
                Birthday = today.AddYears(20),
                Hobbies = []
            }
        };

        var score = FeedScoringService.CalculateScore(
            userProfile,
            candidate,
            Filter.CreateDefault(userProfile.Id));

        score.ShouldBeLessThan(0.05);
    }

    [Theory]
    [InlineData(0, 1.0)]
    [InlineData(FilterDistance / 2.0, 0.607)]
    [InlineData(FilterDistance, 0.135)]
    [InlineData(FilterDistance * 1.1, 0.089)]
    public void CalculateScore_DistanceImpact(int distanceMeters, double expectedDistanceScore)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var userProfile = new User
        {
            Id = Guid.NewGuid(),
            Birthday = today,
            Hobbies = []
        };

        var candidate = new FeedCandidate
        {
            DistanceMeters = distanceMeters,
            Rating = 0.5m,
            User = new User
            {
                Id = Guid.NewGuid(),
                Birthday = today.AddYears(1000),
                Hobbies = []
            }
        };

        var filter = new Filter
        {
            UserId = userProfile.Id,
            RadiusMeters = FilterDistance,
            AgeFrom = null,
            AgeTo = null,
            Gender = null,
            HobbyIds = []
        };

        var score = FeedScoringService.CalculateScore(userProfile, candidate, filter);

        var expectedTotal = (DistanceWeight * expectedDistanceScore);
        score.ShouldBe(expectedTotal, tolerance: 0.01);
    }

    [Theory]
    [InlineData(0, 1.0)]
    [InlineData(365 * 6, 0.607)]
    [InlineData(365 * 8, 0.411)]
    [InlineData(365 * 10, 0.249)]
    [InlineData(365 * 15, 0.043)]
    public void CalculateScore_AgeImpact(int daysDiff, double expectedAgeScore)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var userProfile = new User
        {
            Id = Guid.NewGuid(),
            Birthday = today,
            Hobbies = []
        };

        var candidate = new FeedCandidate
        {
            DistanceMeters = 100000,
            Rating = 0.5m,
            User = new User
            {
                Id = Guid.NewGuid(),
                Birthday = today.AddDays(-daysDiff),
                Hobbies = []
            }
        };

        var score = FeedScoringService.CalculateScore(
            userProfile,
            candidate,
            Filter.CreateDefault(userProfile.Id));

        var expectedTotal = (AgeWeight * expectedAgeScore);
        score.ShouldBe(expectedTotal, tolerance: 0.01);
    }

    [Fact]
    public void CalculateScore_HobbiesImpact_PartialMatch()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var h1 = new Hobby { Id = Guid.NewGuid(), Name = "H1" };
        var h2 = new Hobby { Id = Guid.NewGuid(), Name = "H2" };
        var h3 = new Hobby { Id = Guid.NewGuid(), Name = "H3" };

        var userProfile = new User
        {
            Id = Guid.NewGuid(),
            Birthday = today,
            Hobbies = [h1, h2, h3]
        };

        var candidate = new FeedCandidate
        {
            DistanceMeters = 100000,
            Rating = 0.5m,
            User = new User
            {
                Id = Guid.NewGuid(),
                Birthday = today.AddYears(20),
                Hobbies = [h1, h2]
            }
        };

        var score = FeedScoringService.CalculateScore(
            userProfile,
            candidate,
            Filter.CreateDefault(userProfile.Id));

        var expectedHobbiesScore = 0.667;
        var expectedTotal = HobbiesWeight * expectedHobbiesScore;
        score.ShouldBe(expectedTotal, tolerance: 0.01);
    }
}