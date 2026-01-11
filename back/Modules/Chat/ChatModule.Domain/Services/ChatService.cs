using ChatModule.Domain.Aggregates;
using ChatModule.Domain.Exceptions;
using ChatModule.Domain.Repositories;
using SharedCore.Services;
using SharedCore.Services.LifeTimes;

namespace ChatModule.Domain.Services;

public interface IChatService
{
    public Task<Chat> CreateChatAsync(Guid userA, Guid userB, CancellationToken ct = default);
    public Task<Chat?> GetActiveChatAsync(Guid userId, CancellationToken ct = default);
    public Task AddNewMessageAsync(Guid userId, string messageText, CancellationToken ct = default);
    public Task DeleteChatAsync(Guid userId, CancellationToken ct = default);
}

internal class ChatService : DomainService<IChatRepository>, IChatService, IScoped
{
    private readonly IActiveChatUserLinkRepository _activeChatUserLinkRepository;
    private readonly TimeProvider _timeProvider;

    public ChatService(
        IChatRepository repository,
        IActiveChatUserLinkRepository activeChatUserLinkRepository,
        TimeProvider timeProvider) : base(repository)
    {
        _activeChatUserLinkRepository = activeChatUserLinkRepository;
        _timeProvider = timeProvider;
    }

    public async Task<Chat> CreateChatAsync(Guid userA, Guid userB, CancellationToken ct = default)
    {
        if (userA == userB)
        {
            throw new InvalidOperationException("Users must be different");
        }

        var chat = new Chat
        {
            Id = Guid.CreateVersion7(),
            CreatedAt = _timeProvider.GetUtcNow(),
            IsDeleted = false
        };

        chat.AddUserLink(new ActiveChatUserLink { UserId = userA, ChatId = chat.Id });
        chat.AddUserLink(new ActiveChatUserLink { UserId = userB, ChatId = chat.Id });

        await Repository.AddAsync(chat, ct);
        await Repository.SaveChangesAsync(ct);

        return chat;
    }

    public Task<Chat?> GetActiveChatAsync(Guid userId, CancellationToken ct = default)
    {
        return Repository.GetActiveChatByUserId(userId, ct);
    }

    public async Task AddNewMessageAsync(Guid userId, string messageText, CancellationToken ct = default)
    {
        var chat = await Repository.GetActiveChatByUserId(userId, ct);
        if (chat == null)
        {
            throw ChatExceptions.ChatNotFound(userId);
        }

        var message = new ChatMessage
        {
            Id = Guid.CreateVersion7(),
            ChatId = chat.Id,
            FromUserId = userId,
            MessageText = messageText,
            CreatedAt = _timeProvider.GetUtcNow()
        };

        chat.Messages.Add(message);
        await Repository.SaveChangesAsync(ct);
    }

    public async Task DeleteChatAsync(Guid userId, CancellationToken ct = default)
    {
        var transaction = await Repository
            .And(_activeChatUserLinkRepository)
            .StartTransactionAsync(ct);

        var chat = await Repository.GetActiveChatByUserId(userId, ct);
        if (chat == null)
        {
            return;
        }

        chat.IsDeleted = true;

        var activeChatUserLinks = chat.UserLinks;
        foreach (var activeChatUserLink in activeChatUserLinks)
        {
            _activeChatUserLinkRepository.Delete(activeChatUserLink);
        }

        await transaction.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }
}