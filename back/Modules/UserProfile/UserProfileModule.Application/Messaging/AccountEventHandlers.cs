using SecurityModule.Contracts.Events;
using UserProfileModule.Domain.Repositories;
using Wolverine.Attributes;

namespace UserProfileModule.Application.Messaging;

[WolverineHandler]
public static class AccountEventHandlers
{
    public static async Task HandleAsync(
        AccountDeletedEvent @event,
        IUserRepository userRepository,
        CancellationToken ct)
    {
        var user = await userRepository.GetByIdAsync(@event.Id, asNoTracking: true, ct: ct);
        if (user != null)
        {
            user.IsDeleted = true;
            await userRepository.SaveChangesAsync(ct);
        }
    }
}
