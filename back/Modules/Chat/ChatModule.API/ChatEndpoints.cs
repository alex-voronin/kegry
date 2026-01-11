using System.Security.Claims;
using ChatModule.API.Dto;
using ChatModule.Domain.Aggregates;
using ChatModule.Domain.Exceptions;
using ChatModule.Domain.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Shared.Infrastructure.Storage;
using SharedCore.Api;
using SharedCore.Api.Dto;
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
        group.MapPost("/api/v1/chat/message", SendMessage);
        group.MapDelete("/api/v1/chat", DeleteChat);
    }

    private static async Task<ApiResult<GetChatResponse>> GetActiveChat(
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

        return ApiResult<GetChatResponse>.Build(
            new GetChatResponse(
                Id: chat.Id,
                CreatedAt: chat.CreatedAt,
                Participant: participantDto,
                Messages: messageDtos
            )
        );
    }

    private static async Task<ApiResult.EmptyResult> SendMessage(
        [FromBody] SendMessageRequest request,
        ClaimsPrincipal user,
        IChatService chatService,
        CancellationToken ct)
    {
        var userId = user.GetId();
        await chatService.AddNewMessageAsync(userId, request.MessageText, ct);
        return ApiResult.Empty;
    }

    private static async Task<ApiResult.EmptyResult> DeleteChat(
        ClaimsPrincipal user,
        IChatService chatService,
        CancellationToken ct)
    {
        var userId = user.GetId();
        await chatService.DeleteChatAsync(userId, ct);
        return ApiResult.Empty;
    }

    private static UserDto DomainUserToDto(User user, IS3StorageService storageService)
    {
        return new UserDto(
            Id: user.Id,
            FirstName: user.FirstName,
            LastName: user.LastName,
            AvatarUrl: storageService.S3KeyToPublicFileUrl(user.AvatarImageS3Key)
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