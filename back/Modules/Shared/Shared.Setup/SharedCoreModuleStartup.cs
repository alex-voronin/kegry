using JasperFx.Core.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shared.Infrastructure.Interfaces;
using Shared.Infrastructure.Storage.Configs;

namespace Shared.Setup;

public class SharedCoreModuleStartup : IFeatureStartup
{
    public void Register(IHostApplicationBuilder appBuilder, IHostBuilder host)
    {
        appBuilder.Services.AutoRegisterAssemblies([typeof(Infrastructure.AssemblyMarker).Assembly]);

        var s3Section = appBuilder.Configuration.GetSection("Storage:S3Config");
        appBuilder.Services.AddOptions<S3Config>()
            .Bind(s3Section)
            .ValidateDataAnnotations()
            .ValidateOnStart();
    }

    public void Initialize(WebApplication app)
    {
    }
}