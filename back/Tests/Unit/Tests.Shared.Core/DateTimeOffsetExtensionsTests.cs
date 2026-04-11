using SharedCore.Extensions;
using Shouldly;

namespace Tests.Shared.Core;

public class DateTimeOffsetExtensionsTests
{
    private readonly TimeZoneInfo _moscow = TimeZoneInfo.FindSystemTimeZoneById("Europe/Moscow");
    private readonly TimeZoneInfo _london = TimeZoneInfo.FindSystemTimeZoneById("Europe/London");

    [Fact]
    public void GetUserDayBoundsInUtc_WhenNormalDay_ThenReturnsCorrectBounds()
    {
        var utcTime = new DateTimeOffset(2026, 4, 11, 10, 0, 0, TimeSpan.Zero);
        var (start, end) = utcTime.GetDayBoundsInUtc(_moscow);
        start.ShouldBe(new DateTime(2026, 4, 10, 21, 0, 0, DateTimeKind.Utc));
        end.ShouldBe(new DateTime(2026, 4, 11, 21, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void GetUserDayBoundsInUtc_WhenDstTransitionDay_ThenHandlesCorrectly()
    {
        // DST day in UK
        var utcTime = new DateTimeOffset(2025, 3, 30, 1, 0, 0, TimeSpan.Zero);
        var (start, end) = utcTime.GetDayBoundsInUtc(_london);
        end.ShouldBe(start.AddDays(1));
    }

    [Fact]
    public void GetUserDayBoundsInUtc_WhenMidnightEdgeCase_ThenHandlesCorrectly()
    {
        var utcTime = new DateTimeOffset(2026, 4, 11, 0, 0, 0, TimeSpan.Zero);
        var (start, end) = utcTime.GetDayBoundsInUtc(_moscow);
        end.ShouldBe(start.AddDays(1));
    }
}