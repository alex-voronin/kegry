using System.ComponentModel.DataAnnotations;

namespace Security.Application.Settings;

public class JwtSettings
{
    [Required(AllowEmptyStrings = false)]
    public required string Secret { get; init; }

    [Required(AllowEmptyStrings = false)]
    public required string Issuer { get; init; }

    [Required(AllowEmptyStrings = false)]
    public required string Audience { get; init; }
}