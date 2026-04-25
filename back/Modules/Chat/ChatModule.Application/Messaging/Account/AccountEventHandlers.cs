using ChatModule.Domain.Repositories;
using ChatModule.Domain.Services;
using SecurityModule.Contracts.Events;
using Serilog;
using Wolverine.Attributes;

namespace ChatModule.Application.Messaging.Account;

[WolverineHandler]
public static class AccountEventHandlers
{
    public static async Task HandleAsync(
        AccountDeletedEvent @event,
        IUserRepository userRepository,
        IChatService chatService,
        CancellationToken ct)
    {
        Log.Information("{HandlerClass} Handling event {@Event}",
            typeof(AccountEventHandlers).FullName, @event);
        await chatService.DeleteChatAsync(@event.Id, ct);
        await userRepository.SoftDeleteAsync(@event.Id, ct);
    }
}
