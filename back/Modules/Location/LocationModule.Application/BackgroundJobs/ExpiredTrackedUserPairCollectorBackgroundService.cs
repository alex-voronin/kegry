using LocationModule.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace LocationModule.Application.BackgroundJobs;

public class ExpiredTrackedUserPairCollectorBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly PeriodicTimer _timer = new(TimeSpan.FromHours(12));

    public ExpiredTrackedUserPairCollectorBackgroundService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (await _timer.WaitForNextTickAsync(stoppingToken))
        {
            // Just in case, as a safety measure.
            // Typically, they should be deleted along with a corresponding chat.
            await CollectExpiredTrackedUserPairs(stoppingToken);
        }
    }

    private async Task CollectExpiredTrackedUserPairs(CancellationToken ct)
    {
        Log.Information("Collecting expired tracked users has started");
        using var scope = _scopeFactory.CreateScope();
        var trackedUserPairRepository = scope.ServiceProvider.GetRequiredService<ITrackedUserPairRepository>();
        await trackedUserPairRepository.DeleteExpired(ct);
        await trackedUserPairRepository.SaveChangesAsync(ct);
        Log.Information("Collecting expired tracked users has finished");
    }
}