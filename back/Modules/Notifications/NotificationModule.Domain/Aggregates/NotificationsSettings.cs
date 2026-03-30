using System.Globalization;
using NotificationModule.Domain.ValueObjects;

namespace NotificationModule.Domain.Aggregates;

public class NotificationsSettings
{
    public required Guid UserId { get; init; }
    public required string Locale { get; set; }
    public required bool PushesEnabled { get; set; }
    public required bool EmailsEnabled { get; set; }

    public CultureInfo GetCultureInfo() =>
        CultureInfo.GetCultureInfo(Locale == SupportedLocales.Serbian ? "sr" : "en");

    public void ApplyCurrentLocale()
    {
        CultureInfo.CurrentCulture = GetCultureInfo();
        CultureInfo.CurrentUICulture = GetCultureInfo();
    }
}