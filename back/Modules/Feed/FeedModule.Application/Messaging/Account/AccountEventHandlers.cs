using FeedModule.Domain.Repositories;
using SecurityModule.Contracts.Events;
using Serilog;
using Wolverine.Attributes;

namespace FeedModule.Application.Messaging.Account;

[WolverineHandler]
public static class AccountEventHandlers
{
    public static Task HandleAsync(
        AccountDeletedEvent @event,
        IFeedUserRelationRepository userRelationRepository,
        CancellationToken ct)
    {
        Log.Information("{HandlerClass} Handling event {@Event}", typeof(AccountEventHandlers).FullName, @event);
        return userRelationRepository.DeleteForUserAsync(@event.Id, ct);
    }
}
