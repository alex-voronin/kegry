using System.Text.Json.Serialization;
using Shared.Infrastructure.Interfaces;

namespace API.Web.Startup;

public class HttpSerializationStartup : IFeatureStartup
{
    public void Register(IHostApplicationBuilder appBuilder, IHostBuilder host)
    {
        // configure serializer for MVC and swagger
        appBuilder.Services.Configure<Microsoft.AspNetCore.Mvc.JsonOptions>(options =>
        {
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });
        
        // configure serializer for minimal API
        appBuilder.Services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });
    }

    public void Initialize(WebApplication app)
    {
    }
}