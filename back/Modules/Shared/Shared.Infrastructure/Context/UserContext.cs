using Microsoft.AspNetCore.Http;
using Serilog;
using SharedCore.Interfaces;
using SharedCore.Services.LifeTimes;

namespace Shared.Infrastructure.Context;

internal class UserContext : IUserContext, IScoped
{
    private const string TimezoneHeaderName = "X-Timezone-Id";

    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly TimeProvider _timeProvider;

    public TimeZoneInfo UserTimeZone { get; private set; } = TimeZoneInfo.Utc;
    public DateTimeOffset LocalNow => TimeZoneInfo.ConvertTime(_timeProvider.GetUtcNow(), UserTimeZone);

    public UserContext(IHttpContextAccessor httpContextAccessor, TimeProvider timeProvider)
    {
        _httpContextAccessor = httpContextAccessor;
        _timeProvider = timeProvider;
        InitializeFromHeaders();
    }

    private void InitializeFromHeaders()
    {
        var context = _httpContextAccessor.HttpContext;
        if (context == null)
        {
            UserTimeZone = TimeZoneInfo.Utc;
            return;
        }

        var isHeaderFound = context.Request.Headers.TryGetValue(TimezoneHeaderName, out var values);
        if (!isHeaderFound)
        {
            UserTimeZone = TimeZoneInfo.Utc;
            return;
        }

        var tzId = values.ToString().Trim();
        if (string.IsNullOrEmpty(tzId))
        {
            return;
        }

        try
        {
            UserTimeZone = TimeZoneInfo.FindSystemTimeZoneById(tzId);
        }
        catch (Exception e)
        {
            Log.Error(e, "Error getting timezone info");
            UserTimeZone = TimeZoneInfo.Utc;
        }
    }
}
