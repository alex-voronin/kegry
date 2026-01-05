using System.Globalization;
using Humanizer;
using Microsoft.Extensions.Localization;

namespace SharedCore.Localization;

public interface IDateTimeHumanizer
{
    string Humanize(DateTimeOffset dateTime);
}

public class DateTimeHumanizer : IDateTimeHumanizer
{
    private readonly IStringLocalizer _localizer;
    private readonly CultureInfo _culture;

    public DateTimeHumanizer(
        IStringLocalizer<DateTimeHumanizer> localizer)
    {
        _localizer = localizer;
        _culture = CultureInfo.CurrentUICulture;
    }

    public string Humanize(DateTimeOffset dateTime)
    {
        var now = DateTimeOffset.UtcNow;
        var span = dateTime - now;
        var duration = span.Duration().Humanize(culture: _culture);
        
        if (span.TotalSeconds >= 0)
        {
            return _localizer.GetString("FutureDate", duration);
        }
        return _localizer.GetString("PastDate", duration);
    }
}