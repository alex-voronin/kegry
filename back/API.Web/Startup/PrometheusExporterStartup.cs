using Prometheus;
using Shared.Infrastructure.Interfaces;

namespace API.Web.Startup;

public class PrometheusExporterStartup : IFeatureStartup
{
    public void Register(IHostApplicationBuilder appBuilder, IHostBuilder host)
    {
        var metricsServerConfigSection = appBuilder.Configuration
            .GetSection($"Monitoring:MetricsServerConfig");
        appBuilder.Services.AddMetricServer(options =>
            {
                options.Hostname = metricsServerConfigSection["Hostname"]!;
                options.Port = metricsServerConfigSection.GetValue<ushort>("Port");
            }
        );
    }

    public void Initialize(WebApplication app)
    {
        app.UseHttpMetrics();
    }
}