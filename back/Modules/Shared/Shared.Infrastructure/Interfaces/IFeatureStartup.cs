using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;

namespace Shared.Infrastructure.Interfaces;

public interface IFeatureStartup
{
    public void Register(IHostApplicationBuilder appBuilder, IHostBuilder host);
    public void Initialize(WebApplication app);
}