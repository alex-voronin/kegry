using System.Reflection;
using Microsoft.AspNetCore.Http;

namespace VenueModule.API.B2C.Dto;

public record VenueFilterDto(
    Guid[]? TagIds = null,
    string? CompanyName = null
)
{
    public static ValueTask<VenueFilterDto?> BindAsync(HttpContext context, ParameterInfo parameter)
    {
        var tagIdsStr = context.Request.Query["Filter.TagIds"];
        var companyName = context.Request.Query["Filter.CompanyName"].FirstOrDefault();

        Guid[]? tagIds = null;
        if (!string.IsNullOrEmpty(tagIdsStr))
        {
            tagIds = tagIdsStr.SelectMany(s => s.Split(',')).Select(Guid.Parse).ToArray();
        }

        var result = new VenueFilterDto(
            TagIds: tagIds,
            CompanyName: string.IsNullOrEmpty(companyName) ? null : companyName
        );

        return ValueTask.FromResult<VenueFilterDto?>(result);
    }
}
