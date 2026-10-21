using System.Security.Claims;
using ChatModule.API.Dto;
using ChatModule.Domain.Aggregates;
using ChatModule.Domain.Entities;
using ChatModule.Domain.Exceptions;
using ChatModule.Domain.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Shared.Infrastructure.Storage;
using SharedCore.Api;
using SharedCore.Extensions;

namespace ChatModule.API;

public class ChatEndpoints : IEndpoints
{
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("")
            .WithTags("Chat API")
            .RequireAuthorization();

        group.MapGet("/api/v1/chat", GetActiveChat);
        group.MapGet("/api/v1/chat/status", GetChatStatus);
        group.MapPost("/api/v1/chat/message", SendMessage);
        group.MapDelete("/api/v1/chat", DeleteChat);
    }

    private static async Task<GetChatResponse> GetActiveChat(
        ClaimsPrincipal user,
        IChatService chatService,
        IS3StorageService storageService,
        CancellationToken ct)
    {
        var userId = user.GetId();
        var chat = await chatService.GetActiveChatAsync(userId, ct);

        if (chat == null)
        {
            throw ChatExceptions.ChatNotFound(userId);
        }

        var participantLink = chat.UserLinks.FirstOrDefault(ul => ul.UserId != userId);
        if (participantLink == null || participantLink.User == null)
        {
            throw ChatExceptions.ChatParticipantNotFound(userId);
        }

        var participant = participantLink.User;
        var participantDto = DomainUserToDto(participant, storageService);
        var messageDtos = chat.Messages
            .OrderBy(m => m.CreatedAt)
            .Select(m => DomainMessageToDto(userId, m))
            .ToList();

        return new GetChatResponse(
            Id: chat.Id,
            CreatedAt: chat.CreatedAt,
            ExpiresAt: chat.ExpiresAt,
            MaxMessagesPerUser: ValidationParameters.MaxMessagesPerUser,
            ParticipantsMet: chat.ParticipantsMet,
            Participant: participantDto,
            Messages: messageDtos
            );
    }

    private static async Task<GetChatStatusResponse> GetChatStatus(
        ClaimsPrincipal user,
        IChatService chatService,
        CancellationToken ct)
    {
        var userId = user.GetId();
        var hasActiveChat = await chatService.HasActiveChatAsync(userId, ct);
        return new GetChatStatusResponse(HasActiveChat: hasActiveChat);
    }

    private static async Task<NoContent> SendMessage(
        [FromBody] SendMessageRequest request,
        ClaimsPrincipal user,
        IChatService chatService,
        CancellationToken ct)
    {
        var userId = user.GetId();
        await chatService.AddNewMessageAsync(userId, request.MessageText, ct);
        return TypedResults.NoContent();
    }

    private static async Task<NoContent> DeleteChat(
        ClaimsPrincipal user,
        IChatService chatService,
        CancellationToken ct)
    {
        var userId = user.GetId();
        await chatService.DeleteChatAsync(userId, ct);
        return TypedResults.NoContent();
    }

    private static UserDto DomainUserToDto(User user, IS3StorageService storageService)
    {
        return new UserDto(
            Id: user.Id,
            FirstName: user.FirstName,
            LastName: user.LastName,
            AvatarUrl: storageService.S3KeyToPublicFileUrl(user.AvatarImageS3Key, user.UpdatedAt)
        );
    }

    private static ChatMessageDto DomainMessageToDto(Guid userId, ChatMessage message)
    {
        return new ChatMessageDto(
            Id: message.Id,
            MessageText: message.Text,
            CreatedAt: message.CreatedAt,
            IsMine: message.FromUserId == userId
        );
    }
}