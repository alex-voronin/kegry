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
        result.visitsLowerBound.ShouldBe(0);
        result.visitsUpperBound.ShouldBe(null);
    }

    [Theory]
    // visitsCount, expectedLevelId, expectedLowerBound, expectedUpperBound
    [InlineData(0, 0, 0, 5)]
    [InlineData(4, 0, 0, 5)]
    [InlineData(5, 1, 5, 10)]
    [InlineData(10, 2, 10, 15)]
    [InlineData(14, 2, 10, 15)]
    [InlineData(20, 3, 20, -1)]
    [InlineData(25, 3, 20, -1)]
    public void GetCurrentLoyaltyLevel_WithVariousVisits_ReturnsExpectedResult(
        int visitsCount, int expectedLevelId, int expectedLowerBound, int expectedUpperBound)
    {
        var companyId = Guid.NewGuid();
        var levels = new List<LoyaltyLevel>
        {
            new LoyaltyLevel { Id = 0, CompanyId = companyId, VisitsUpperBound = 5, IsActive = true},
            new LoyaltyLevel { Id = 1, CompanyId = companyId, VisitsUpperBound = 10, IsActive = true },
            new LoyaltyLevel { Id = 2, CompanyId = companyId, VisitsUpperBound = 15, IsActive = true },
            new LoyaltyLevel { Id = 3, CompanyId = companyId, VisitsUpperBound = 20, IsActive = true }
        };
        var result = LoyaltyService.GetCurrentLoyaltyLevel(levels, visitsCount);
        result.currentLoyaltyLevel.ShouldBe(expectedLevelId);
        result.visitsLowerBound.ShouldBe(expectedLowerBound);
        result.visitsUpperBound.ShouldBe(expectedUpperBound);
    }
}
