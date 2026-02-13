using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using PostHog;
using SharedCore.Services.LifeTimes;

namespace AnalyticsModule.Application.Services;

public interface IAnalyticsService
{
    public Task IdentifyAsync(
        string distinctId,
        string email,
        string name);
    
    public bool Capture(
        string distinctId,
        string eventName,
        Dictionary<string, object>? properties = null);
}

internal class PosthogAnalyticsService : IAnalyticsService, ISingleton
{
    private readonly IWebHostEnvironment _hostEnvironment;
    private readonly IPostHogClient _postHogClient;

    public PosthogAnalyticsService(IWebHostEnvironment hostEnvironment, IPostHogClient postHogClient)
    {
        _hostEnvironment = hostEnvironment;
        _postHogClient = postHogClient;
    }

    public bool Capture(
        string distinctId,
        string eventName,
        Dictionary<string, object>? properties)
    {
        properties ??= new Dictionary<string, object>();
        properties[AnalyticsEvents.Keys.Environment] = GetAnalyticsEnvName();
        
        return _postHogClient.Capture(
            distinctId: distinctId,
            eventName: eventName,
            properties: properties);
    }
    
    public Task IdentifyAsync(
        string distinctId,
        string email,
        string name)
    {
        var properties = new Dictionary<string, object>
        {
            [AnalyticsEvents.Keys.Environment] = GetAnalyticsEnvName()
        };

        return _postHogClient.IdentifyAsync(
            distinctId: distinctId,
            email: email,
            name: name,
            personPropertiesToSet: properties);
    }
    
    private string GetAnalyticsEnvName()
    {
        return _hostEnvironment.IsDevelopment() ? "dev" : "prod";
    }
}