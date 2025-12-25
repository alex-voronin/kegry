using Microsoft.AspNetCore.ResponseCompression;
using Shared.Infrastructure.Interfaces;

namespace API.Web.Startup;

public class ResponseCompressionStartup : IFeatureStartup
{
    public void Register(IHostApplicationBuilder appBuilder, IHostBuilder host)
    {
        var services = appBuilder.Services;
            
        services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
            options.Providers.Add<BrotliCompressionProvider>();
            options.Providers.Add<GzipCompressionProvider>();
        });

        services.Configure<BrotliCompressionProviderOptions>(options =>
        {
            options.Level = System.IO.Compression.CompressionLevel.Fastest;
        });

        services.Configure<GzipCompressionProviderOptions>(options =>
        {
            options.Level = System.IO.Compression.CompressionLevel.Fastest;
        });
    }

    public void Initialize(WebApplication app)
    {
        app.UseResponseCompression();
    }
}