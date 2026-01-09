using System.ComponentModel.DataAnnotations;

namespace UserProfileModule.Infrastructure.Storage.Configs;

public class S3Config
{
    [Required]
    public required string AccessKey { get; init; }

    [Required]
    public required string SecretKey { get; init; }

    [Required]
    public required string ServiceUrl { get; init; }

    [Required]
    public required string BucketName { get; init; }

    [Required]
    public required string PublicUrl { get; init; }

    [Required]
    public required string KeySalt { get; init; }

    [Required]
    public required bool UseHttp { get; init; } = false;
}
