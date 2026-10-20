using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace IntegrationTests.Shared;

public static class ProblemDetailsTestExtensions
{
    public static string? GetCode(this ProblemDetails problem)
        => problem.GetExtension("code");

    public static Guid? GetVenueId(this ProblemDetails problem)
        => Guid.TryParse(problem.GetExtension("venueId"), out var id) ? id : null;

    private static string? GetExtension(this ProblemDetails problem, string key)
    {
        if (!problem.Extensions.TryGetValue(key, out var value) || value is null)
        {
            return null;
        }

        return value switch
        {
            JsonElement { ValueKind: JsonValueKind.String } element => element.GetString(),
            JsonElement element => element.ToString(),
            _ => value.ToString()
        };
    }
}
