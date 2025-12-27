using System.ComponentModel.DataAnnotations;

namespace Notifications.Infrastructure.Transport.Email;

public class EmailConfig
{
    [Required(AllowEmptyStrings = false)]
    public required string Domain { get; init; }
    
    [Required(AllowEmptyStrings = false)]
    public required string FromEmail { get; init; }
    
    [Required(AllowEmptyStrings = false)]
    public required string ApiKey { get; init; }
}