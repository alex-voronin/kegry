using ChatModule.Contracts.Events;
using ChatModule.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace ChatModule.Application.BackgroundJobs;

public class ExpiredChatsCollectorBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly PeriodicTimer _timer = new(TimeSpan.FromHours(1));

    public ExpiredChatsCollectorBackgroundService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (await _timer.WaitForNextTickAsync(stoppingToken))
        {
            await CollectExpiredChats(stoppingToken);
        }
    }

    private async Task CollectExpiredChats(CancellationToken ct)
    {
        Log.Information("Collecting expired chat has started");
        using var scope = _scopeFactory.CreateScope();
        var chatRepository = scope.ServiceProvider.GetRequiredService<IChatRepository>();
        var activeChatUserLinkRepository = scope.ServiceProvider.GetRequiredService<IActiveChatUserLinkRepository>();

        var transaction = await chatRepository.And(activeChatUserLinkRepository)
            .StartTransactionAsync(ct);
        await chatRepository.MarkExpiredChatsAsDeleted(ct);

        IReadOnlyList<Guid[]> userIdsFromDeletedChats;
        do
        {
            userIdsFromDeletedChats = await activeChatUserLinkRepository
                .BatchDeleteLinksToDeletedChats(100, ct);
            var @event = new ChatsDeletedEvent()
            {
                ParticipantIdsFromDeletedChats = userIdsFromDeletedChats
            };
            await chatRepository.PublishEventAsync(@event, ct);
        } while (userIdsFromDeletedChats.Count > 0);

        await transaction.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        Log.Information("Collecting expired chat has finished");
    }
}