using System.ComponentModel.DataAnnotations;

namespace SecurityModule.Application.Configs;

public class GoogleAuthConfig
{
    [Required(AllowEmptyStrings = false)]
    public required string ClientId { get; init; }
}