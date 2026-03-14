using System.Reflection;
using Shouldly;
using VenueModule.Domain.Aggregates;
using VenueModule.Domain.Services;

namespace Tests.VenueModule.Domain;

public class LoyaltyServiceTests
{

    [Fact]
    public void GetCurrentLoyaltyLevel_WhenLevelsEmpty_ReturnsZeroes()
    {
        var levels = Array.Empty<LoyaltyLevel>();
        var result = LoyaltyService.GetCurrentLoyaltyLevel(levels, 0);
        result.currentLoyaltyLevel.ShouldBe(0);
        result.visitsToNextLoyaltyLevel.ShouldBe(0);
    }

    [Theory]
    // visitsCount, expectedLevelId, expectedVisitsNeeded
    [InlineData(0, 0, 5)]
    [InlineData(4, 0, 1)]
    [InlineData(5, 1, 5)]
    [InlineData(10, 2, 5)]
    [InlineData(14, 2, 1)]
    [InlineData(20, 3, 0)]
    [InlineData(25, 3, 0)]
    public void GetCurrentLoyaltyLevel_WithVariousVisits_ReturnsExpectedResult(
        int visitsCount, int expectedLevelId, int expectedVisitsNeeded)
    {
        var levels = new List<LoyaltyLevel>
        {
            new LoyaltyLevel { Id = 0, VisitsUpperBound = 5 },
            new LoyaltyLevel { Id = 1, VisitsUpperBound = 10 },
            new LoyaltyLevel { Id = 2, VisitsUpperBound = 15 },
            new LoyaltyLevel { Id = 3, VisitsUpperBound = 20 }
        };
        var result = LoyaltyService.GetCurrentLoyaltyLevel(levels, visitsCount);
        result.currentLoyaltyLevel.ShouldBe(expectedLevelId);
        result.visitsToNextLoyaltyLevel.ShouldBe(expectedVisitsNeeded);
    }
}
