namespace SharedCore.Extensions;

public static class DateTimeOffsetExtensions
{
    public static (DateTimeOffset startOfDayUtc, DateTimeOffset endOfDayUtc) GetDayBoundsInUtc(
        this DateTimeOffset fromTime, TimeZoneInfo timeZone)
    {
        var userLocalNow = TimeZoneInfo.ConvertTime(fromTime, timeZone);

        var userDateStartLocal = new DateTimeOffset(
            year: userLocalNow.Year,
            month: userLocalNow.Month,
            day: userLocalNow.Day,
            hour: 0,
            minute: 0,
            second: 0,
            offset: timeZone.GetUtcOffset(userLocalNow)
        );
        var startOfDayUtc = userDateStartLocal.ToUniversalTime();
        var endOfDayUtc = startOfDayUtc.AddDays(1);
        return (startOfDayUtc, endOfDayUtc);
    }
}