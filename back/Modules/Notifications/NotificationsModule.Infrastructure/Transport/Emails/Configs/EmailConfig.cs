using System.ComponentModel.DataAnnotations;

namespace NotificationsModule.Infrastructure.Transport.Email.Configs;

public class EmailConfig
{
    [Required(AllowEmptyStrings = false)]
    public required string AppId { get; init; }

    [Required(AllowEmptyStrings = false)]
    public required string FromEmail { get; init; }

    [Required(AllowEmptyStrings = false)]
    public required string ApiKey { get; init; }
}