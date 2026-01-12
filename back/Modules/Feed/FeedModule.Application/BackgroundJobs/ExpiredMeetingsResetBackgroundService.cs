using FeedModule.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace FeedModule.Application.BackgroundJobs;

public class ExpiredMeetingsResetBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly PeriodicTimer _timer = new(TimeSpan.FromHours(12));

    public ExpiredMeetingsResetBackgroundService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (await _timer.WaitForNextTickAsync(stoppingToken))
        {
            // Just in case, as a safety measure.
            // Typically, the IsInMeeting flag must be reset via events.
            await ResetExpiredMeetings(stoppingToken);
        }
    }

    private async Task ResetExpiredMeetings(CancellationToken ct)
    {
        Log.Information("Resetting expired meetings has started");
        using var scope = _scopeFactory.CreateScope();
        var userRelationRepository = scope.ServiceProvider.GetRequiredService<IFeedUserRelationRepository>();
        await userRelationRepository.ResetExpiredMeetingsAsync(ct);
        await userRelationRepository.SaveChangesAsync(ct);
        Log.Information("Resetting expired meetings has finished");
    }
}