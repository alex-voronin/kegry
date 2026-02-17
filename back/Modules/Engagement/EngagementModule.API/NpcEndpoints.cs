using System.Security.Claims;
using EngagementModule.API.Dto;
using EngagementModule.Domain.Services;
using EngagementModule.Infrastructure.Resources;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Localization;
using SharedCore.Api;
using SharedCore.Api.Dto;
using SharedCore.Extensions;

namespace EngagementModule.API;

public class NpcEndpoints : IEndpoints
{
    public static void MapEndpoints(IEndpointRouteBuilder builder)
    {
        var apiGroup = builder.MapGroup("")
            .WithTags("NPC API")
            .RequireAuthorization();
        apiGroup.MapGet("/api/v1/npc", GetNpc);
        apiGroup.MapGet("/api/v1/npc/{id:guid}/invite", InviteNpc);
    }

    private static async Task<ApiResult<GetNpcResponse>> GetNpc(
        ClaimsPrincipal userClaims,
        INpcActionsService npcActionsService,
        IStringLocalizer<NpcLocalization> localizer,
        CancellationToken ct)
    {
        var npc = await npcActionsService.GetNpcAsync(userClaims.GetId(), ct);
        var npcDto = npc == null
            ? null
            : new NpcDto(
                Id: npc.Id.ToString(),
                Name: localizer.GetString(npc.NameKey),
                AvatarUrl: npc.AvatarUrl
            );
        return ApiResult<GetNpcResponse>.Build(
            new GetNpcResponse(npcDto)
        );
    }

    private static async Task<ApiResult<InviteNpcResponse>> InviteNpc(
        [FromRoute] Guid id,
        INpcActionsService npcActionsService,
        ClaimsPrincipal user,
        IStringLocalizer<NpcLocalization> localizer,
        CancellationToken ct)
    {
        var npc = await npcActionsService.InviteNpcAsync(user.GetId(), id, ct);
        var response = new InviteNpcResponse(Monolog: localizer.GetString(npc.MonologKey));
        return ApiResult<InviteNpcResponse>.Build(response);
    }
}