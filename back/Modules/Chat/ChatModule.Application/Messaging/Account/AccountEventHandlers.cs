using ChatModule.Domain.Repositories;
using SecurityModule.Contracts.Events;
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
        return userRepository.SoftDeleteAsync(@event.Id, ct);
    }
}
