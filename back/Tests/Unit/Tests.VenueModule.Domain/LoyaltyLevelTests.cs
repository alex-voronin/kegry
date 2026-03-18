using SharedCore.Exceptions;
using Shouldly;
using VenueModule.Domain.Aggregates;

namespace Tests.VenueModule.Domain;

public class LoyaltyLevelTests
{
    // ── Id validation ─────────────────────────────────────────────────────────

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void LoyaltyLevel_WhenIdIsValid_ThenInitializes(int validId)
    {
        var level = new LoyaltyLevel { Id = validId, CompanyId = Guid.NewGuid(), VisitsUpperBound = 5 };
        level.Id.ShouldBe(validId);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(4)]
    [InlineData(100)]
    public void LoyaltyLevel_WhenIdIsOutOfRange_ThenThrows(int invalidId)
    {
        Should.Throw<HttpCodeException>(() => _ = new LoyaltyLevel
                { Id = invalidId, CompanyId = Guid.NewGuid(), VisitsUpperBound = 5 })
            .ErrorCode.ShouldBe(ErrorCode.ValidationError);
    }

    // ── VisitsUpperBound validation ───────────────────────────────────────────

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    public void LoyaltyLevel_WhenVisitsUpperBoundIsPositive_ThenInitializes(int value)
    {
        var level = new LoyaltyLevel { Id = 0, CompanyId = Guid.NewGuid(), VisitsUpperBound = value };
        level.VisitsUpperBound.ShouldBe(value);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    public void LoyaltyLevel_WhenVisitsUpperBoundIsNotPositive_ThenThrows(int invalidValue)
    {
        Should.Throw<HttpCodeException>(() => _ = new LoyaltyLevel
                { Id = 0, CompanyId = Guid.NewGuid(), VisitsUpperBound = invalidValue })
            .ErrorCode.ShouldBe(ErrorCode.ValidationError);
    }

    // ── ValidateCollection — count ────────────────────────────────────────────

    [Fact]
    public void ValidateCollection_WhenEmpty_ThenDoesNotThrow()
    {
        Should.NotThrow(() => LoyaltyLevel.ValidateLevelsCountAndOrder([]));
    }

    [Fact]
    public void ValidateCollection_WhenThreeLevels_ThenDoesNotThrow()
    {
        var levels = new[]
        {
            MakeLevel(0, 5),
            MakeLevel(1, 10),
            MakeLevel(2, 20),
        };
        Should.NotThrow(() => LoyaltyLevel.ValidateLevelsCountAndOrder(levels));
    }

    [Fact]
    public void ValidateCollection_WhenMoreThanThreeLevels_ThenThrows()
    {
        // Bypassing property validation to test collection count independently
        var levels = new[]
        {
            MakeLevel(0, 5),
            MakeLevel(1, 10),
            MakeLevel(2, 20),
            MakeLevel(2, 30), // duplicate Id but we're testing count, not Id uniqueness
        };
        Should.Throw<HttpCodeException>(() => LoyaltyLevel.ValidateLevelsCountAndOrder(levels))
            .ErrorCode.ShouldBe(ErrorCode.ValidationError);
    }

    // ── ValidateCollection — ordering ─────────────────────────────────────────

    [Fact]
    public void ValidateCollection_WhenUpperBoundsAreStrictlyAscending_ThenDoesNotThrow()
    {
        var levels = new[]
        {
            MakeLevel(0, 5),
            MakeLevel(1, 15),
        };
        Should.NotThrow(() => LoyaltyLevel.ValidateLevelsCountAndOrder(levels));
    }

    [Fact]
    public void ValidateCollection_WhenUpperBoundsAreEqual_ThenThrows()
    {
        var levels = new[]
        {
            MakeLevel(0, 10),
            MakeLevel(1, 10),
        };
        Should.Throw<HttpCodeException>(() => LoyaltyLevel.ValidateLevelsCountAndOrder(levels))
            .ErrorCode.ShouldBe(ErrorCode.ValidationError);
    }

    [Fact]
    public void ValidateCollection_WhenUpperBoundsAreDescending_ThenThrows()
    {
        var levels = new[]
        {
            MakeLevel(0, 20),
            MakeLevel(1, 10),
        };
        Should.Throw<HttpCodeException>(() => LoyaltyLevel.ValidateLevelsCountAndOrder(levels))
            .ErrorCode.ShouldBe(ErrorCode.ValidationError);
    }

    [Fact]
    public void ValidateCollection_WhenOrderedByIdButBoundsDescending_ThenThrows()
    {
        // Provide levels out of Id order — ValidateCollection sorts by Id before checking
        var levels = new[]
        {
            MakeLevel(2, 5), // highest Id, lowest bound
            MakeLevel(0, 20), // lowest Id, highest bound
        };
        Should.Throw<HttpCodeException>(() => LoyaltyLevel.ValidateLevelsCountAndOrder(levels))
            .ErrorCode.ShouldBe(ErrorCode.ValidationError);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static LoyaltyLevel MakeLevel(int id, int visitsUpperBound) =>
        new() { Id = id, CompanyId = Guid.NewGuid(), VisitsUpperBound = visitsUpperBound };
}