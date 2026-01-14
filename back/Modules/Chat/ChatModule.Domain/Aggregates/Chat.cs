using ChatModule.Domain.Entities;
using SharedCore.Interfaces;
using SharedCore.Validation;

namespace ChatModule.Domain.Aggregates;

public class Chat : ISoftDeletable<Guid>
{
    public required Guid Id { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset ExpiresAt { get; init; }
    public bool ParticipantsMet { get; set; }
    public bool IsDeleted { get; set; }

    private readonly List<ActiveChatUserLink> _userLinks = new List<ActiveChatUserLink>();
    public IReadOnlyList<ActiveChatUserLink> UserLinks
    {
        get => _userLinks;
        init => _userLinks = new List<ActiveChatUserLink>(value);
    }

    private readonly List<ChatMessage> _messages = new List<ChatMessage>();
    public IReadOnlyList<ChatMessage> Messages
    {
        get => _messages;
        init => _messages = new List<ChatMessage>(value);
    }

    internal Chat()
    {
    }

    public void AddUserLink(ActiveChatUserLink link)
    {
        Validation.ThrowIfInvalid(
            nameof(UserLinks),
            () => UserLinks.Count <= ValidationParameters.MaxUsersPerChat,
            $"Chat doesn't support more than {ValidationParameters.MaxUsersPerChat}  users.");
        _userLinks.Add(link);
    }

    public void AddMessage(ChatMessage newMessage)
    {
        var postedMessageCount = _messages.Count(m => m.FromUserId == newMessage.FromUserId);
        Validation.ThrowIfInvalid(
            nameof(Messages),
            () => postedMessageCount < ValidationParameters.MaxMessagesPerUser,
            $"You are unable to post more than {ValidationParameters.MaxMessagesPerUser} messages per chat");
        _messages.Add(newMessage);
    }
}