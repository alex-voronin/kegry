using System.ComponentModel.DataAnnotations;

namespace SecurityModule.Application.Configs;

public class AuthTokenConfig
{
    [Required(AllowEmptyStrings = false)]
    public required string Secret { get; init; }

    [Required(AllowEmptyStrings = false)]
    public required string Issuer { get; init; }

    [Required(AllowEmptyStrings = false)]
    public required string Audience { get; init; }

    [Range(1, 14)]
    public required int AccessTokenDurationInDays { get; init; }

    [Range(1, 180)]
    public required int RefreshTokenDurationInDays { get; init; }

    [Range(1, 30)]
    public required int MaxSimultaneousLoggedInDevices { get; init; }
}