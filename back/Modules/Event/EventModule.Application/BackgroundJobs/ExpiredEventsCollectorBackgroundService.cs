using EventModule.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Shared.Infrastructure.Storage;

namespace EventModule.Application.BackgroundJobs;

public class ExpiredEventsCollectorBackgroundService : BackgroundService
{
    internal const int SoftDeleteAfterMonths = 3;
    internal const int HardDeleteAfterMonths = 12;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly PeriodicTimer _timer = new(TimeSpan.FromHours(24));

    public ExpiredEventsCollectorBackgroundService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (await _timer.WaitForNextTickAsync(stoppingToken))
        {
            await CollectExpiredEvents(stoppingToken);
        }
    }

    internal async Task CollectExpiredEvents(CancellationToken ct)
    {
        Log.Information("Collecting expired events has started");
        using var scope = _scopeFactory.CreateScope();
        var eventRepository = scope.ServiceProvider.GetRequiredService<IEventRepository>();
        var s3StorageService = scope.ServiceProvider.GetRequiredService<IS3StorageService>();

        var now = DateTimeOffset.UtcNow;
        var threeMonthsAgo = now.AddMonths(-SoftDeleteAfterMonths);
        var twelveMonthsAgo = now.AddMonths(-HardDeleteAfterMonths);

        var coverKeys = await eventRepository
            .GetCoverImageKeysForInactiveEventsAsync(threeMonthsAgo, ct);

        var transaction = await eventRepository
            .And(eventRepository)
            .StartTransactionAsync(ct);

        await eventRepository.SoftDeleteInactiveEventsAsync(threeMonthsAgo, ct);
        await eventRepository.HardDeleteExpiredEventsAsync(twelveMonthsAgo, ct);

        await transaction.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        await eventRepository.FlushOutgoingMessagesAsync();

        foreach (var key in coverKeys)
        {
            await s3StorageService.DeleteObjectAsync(key, dontThrow: true);
        }

        Log.Information("Collecting expired events has finished with {KeyCount} S3 objects removed", coverKeys.Length);
    }
}