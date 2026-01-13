using SecurityModule.Contracts.Events;
using UserProfileModule.Domain.Repositories;
using Wolverine.Attributes;

namespace UserProfileModule.Application.Messaging.Account;

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
