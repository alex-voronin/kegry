using ChatModule.Domain.Repositories;
using SecurityModule.Contracts.Events;
using Serilog;
using Wolverine.Attributes;

namespace ChatModule.Application.Messaging.Account;

[WolverineHandler]
public static class AccountEventHandlers
{
    public static Task HandleAsync(
        AccountDeletedEvent @event,
        IUserRepository userRepository,
        CancellationToken ct)
    {
        Log.Information("{HandlerClass} Handling event {@Event}",
            typeof(AccountEventHandlers).FullName, @event);
        return userRepository.SoftDeleteAsync(@event.Id, ct);
    }
}
