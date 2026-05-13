using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SharedCore.Api;
using SharedCore.Api.Dto;
using SharedCore.Policies;
using VenueModule.API.B2B.Tags.Dto;
using VenueModule.Domain.Repositories;

namespace VenueModule.API.B2B.Tags;

public class B2BTagEndpoints : IEndpoints
{
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var tagsApiGroup = app.MapGroup("")
            .WithTags("Venue Tags Management API")
            .RequireAuthorization(AuthPolicies.B2BAdmin);

        tagsApiGroup.MapGet("/api/v1/tags", GetTags);
    }

    private static async Task<ApiResult<TagCategoryDto[]>> GetTags(
        ITagRepository tagRepository,
        CancellationToken ct)
    {
        var tags = await tagRepository.GetAllAsync(ct);

        var dtos = tags
            .GroupBy(t => new { t.Category, t.IsSingleSelect })
            .Select(g => new TagCategoryDto(
                Category: g.Key.Category.ToString(),
                IsSingleSelect: g.Key.IsSingleSelect,
                Tags: g.OrderBy(t => t.SortOrder)
                       .Select(t => new TagDto(Id: t.Id, Value:t.Value, ParentValue:t.Parent?.Value))
                       .ToArray()
            ))
            .ToArray();

        return ApiResult<TagCategoryDto[]>.Build(dtos);
    }
}
