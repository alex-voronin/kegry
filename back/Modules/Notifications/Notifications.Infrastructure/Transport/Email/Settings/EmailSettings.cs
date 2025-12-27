using System.ComponentModel.DataAnnotations;

namespace Application.Emails.Settings;

public class EmailSettings
{
    [Required(AllowEmptyStrings = false)]
    public required string Domain { get; init; }
    
    [Required(AllowEmptyStrings = false)]
    public required string FromEmail { get; init; }
    
    [Required(AllowEmptyStrings = false)]
    public required string ApiKey { get; init; }
}