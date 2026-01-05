using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using SharedCore.Localization;

namespace Tests.Shared.Core;

public class DateTimeHumanizerTests
{
    private readonly IDateTimeHumanizer _dateTimeHumanizer;

    public DateTimeHumanizerTests()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddLocalization();
        services.AddTransient<IDateTimeHumanizer, DateTimeHumanizer>();
        
        var serviceProvider = services.BuildServiceProvider();
        _dateTimeHumanizer = serviceProvider.GetRequiredService<IDateTimeHumanizer>();
        
        CultureInfo.CurrentUICulture = new CultureInfo("en-US");
    }

    [Theory]
    [InlineData(10, "within 10 seconds")]
    [InlineData(60, "within 1 minute")]
    [InlineData(3600, "within 1 hour")]
    [InlineData(3600 * 24, "within 1 day")]
    [InlineData(3600 * 24 * 7, "within 1 week")]
    [InlineData(3600 * 24 * 7 * 5, "within 1 month")]
    public void Humanize_FutureDates_ReturnsCorrectString(int secondsToAdd, string expected)
    {
        var dateTime = DateTimeOffset.UtcNow.AddSeconds(secondsToAdd).AddMilliseconds(500);
        var result = _dateTimeHumanizer.Humanize(dateTime);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(-10, "10 seconds ago")]
    [InlineData(-60, "1 minute ago")]
    [InlineData(-3600, "1 hour ago")]
    [InlineData(-3600 * 24, "1 day ago")]
    [InlineData(-3600 * 24 * 7, "1 week ago")]
    [InlineData(-3600 * 24 * 7 * 5, "1 month ago")]
    public void Humanize_PastDates_ReturnsCorrectString(int secondsToSubtract, string expected)
    {
        var dateTime = DateTimeOffset.UtcNow.AddSeconds(secondsToSubtract).AddMilliseconds(-500);
        var result = _dateTimeHumanizer.Humanize(dateTime);
        Assert.Equal(expected, result);
    }
}
