using ChatModule.Contracts.Events;
using ChatModule.Domain.Aggregates;
using ChatModule.Domain.Entities;
using ChatModule.Domain.Exceptions;
using ChatModule.Domain.Repositories;
using Shared.Infrastructure.Storage;
using SharedCore.Services;
using SharedCore.Services.LifeTimes;

namespace ChatModule.Domain.Services;

public interface IChatService
{
    public Task<Chat> CreateChatAsync(Guid[] userIds, DateTimeOffset expiresAt, CancellationToken ct = default);

    public Task<Chat?> GetActiveChatAsync(Guid userId, CancellationToken ct = default);
    public Task AddNewMessageAsync(Guid userId, string messageText, CancellationToken ct = default);
    public Task DeleteChatAsync(Guid userId, CancellationToken ct = default);
}

internal class ChatService : DomainService<IChatRepository>, IChatService, IScoped
{
    private readonly IActiveChatUserLinkRepository _activeChatUserLinkRepository;
    private readonly IS3StorageService _storageService;
    private readonly TimeProvider _timeProvider;

    public ChatService(
        IChatRepository repository,
        IActiveChatUserLinkRepository activeChatUserLinkRepository,
        IS3StorageService storageService,
        TimeProvider timeProvider) : base(repository)
    {
        _activeChatUserLinkRepository = activeChatUserLinkRepository;
        _storageService = storageService;
        _timeProvider = timeProvider;
    }

    public async Task<Chat> CreateChatAsync(Guid[] userIds, DateTimeOffset expiresAt, CancellationToken ct = default)
    {
        if (userIds.Length != 2)
        {
            throw new ArgumentException("Only two participants are supported");
        }

        if (userIds.ToHashSet().Count < 2)
        {
            throw new InvalidOperationException("Users must be different");
        }

        var chat = new Chat
        {
            Id = Guid.CreateVersion7(),
            CreatedAt = _timeProvider.GetUtcNow(),
            ExpiresAt = expiresAt,
            IsDeleted = false
        };

        foreach (var userId in userIds)
        {
            chat.AddUserLink(new ActiveChatUserLink { UserId = userId, ChatId = chat.Id });
        }

        await Repository.AddAsync(chat, ct);
        await Repository.SaveChangesAsync(ct);

        return chat;
    }

    public Task<Chat?> GetActiveChatAsync(Guid userId, CancellationToken ct = default)
    {
        return Repository.GetActiveChatByUserId(userId, loadMessages: true, ct: ct);
    }

    public async Task AddNewMessageAsync(Guid userId, string messageText, CancellationToken ct = default)
    {
        // It's cheaper to load the entire chat with its messages in a single query
        // than load the chat without messages and then query the message count,
        // because a chat can't have more than 60 messages.
        var chat = await Repository.GetActiveChatByUserId(userId, loadMessages: true, ct: ct);
        if (chat == null)
        {
            throw ChatExceptions.ChatNotFound(userId);
        }

        var message = new ChatMessage
        {
            Id = Guid.CreateVersion7(),
            ChatId = chat.Id,
            FromUserId = userId,
            Text = messageText,
            CreatedAt = _timeProvider.GetUtcNow()
        };

        chat.AddMessage(message);

        var toUser = chat.UserLinks.First(l => l.UserId != userId).User;
        var fromUser = chat.UserLinks.First(l => l.UserId == userId).User;
        var @event = new ChatMessageSentEvent()
        {
            ToUserId = toUser.Id,
            FromUserId = fromUser.Id,
            FromUserName = fromUser.FirstName,
            FromUserAvatarUrl = _storageService.S3KeyToPublicFileUrl(fromUser.AvatarImageS3Key, fromUser.ChangedAt),
            MessageText = message.Text,
            CreatedAt = message.CreatedAt
        };
        await Repository.PublishEventAsync(@event);
        await Repository.SaveChangesAndFlushMessagesAsync(ct);
    }

    public async Task DeleteChatAsync(Guid userId, CancellationToken ct = default)
    {
        var transaction = await Repository
            .And(_activeChatUserLinkRepository)
            .StartTransactionAsync(ct);

        var chat = await Repository.GetActiveChatByUserId(userId, loadMessages: false, ct: ct);
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

        var participantIds = activeChatUserLinks.Select(l => l.UserId).ToArray();
        var @event = new ChatsDeletedEvent()
        {
            ParticipantIdsFromDeletedChats = [participantIds]
        };
        await Repository.PublishEventAsync(@event);

        await transaction.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        // We must wait until the transaction ends before publishing events,
        // and we cannot call SaveChangesAndFlushMessagesAsync() on the DbContext before the end of the transaction
        // because its implementation commits the transaction.
        await Repository.FlushOutgoingMessagesAsync();
    }
}