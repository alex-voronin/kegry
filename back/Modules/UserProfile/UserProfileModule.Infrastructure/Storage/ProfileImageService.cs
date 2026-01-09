using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using SharedCore.Services.LifeTimes;
using UserProfileModule.Domain.Services;
using UserProfileModule.Infrastructure.Storage.Configs;

namespace UserProfileModule.Infrastructure.Storage;

internal class ProfileImageService : IProfileImageService, ISingleton
{
    private readonly IStorageService _storageService;
    private readonly S3Config _config;

    public ProfileImageService(IStorageService storageService, IOptions<S3Config> config)
    {
        _storageService = storageService;
        _config = config.Value;
    }

    public async Task<StorageUrls> GetAvatarUploadUrlAsync(Guid userId, CancellationToken ct = default)
    {
        var key = GenerateKey(userId, fileName: "avatar");
        var presignedUrl = await _storageService.GetPresignedUploadUrlAsync(
            key,
            "image/jpeg",
            TimeSpan.FromMinutes(10));
        var publicUrl = BuildPublicUrl(key);
        return new StorageUrls(presignedUrl, publicUrl);
    }

    public async Task<StorageUrls> GetCoverUploadUrlAsync(Guid userId, CancellationToken ct = default)
    {
        var key = GenerateKey(userId, fileName: "cover");
        var presignedUrl = await _storageService.GetPresignedUploadUrlAsync(
            key,
            "image/jpeg",
            TimeSpan.FromMinutes(10));
        var publicUrl = BuildPublicUrl(key);
        return new StorageUrls(presignedUrl, publicUrl);
    }

    private string GenerateKey(Guid userId, string fileName)
    {
        var hash = Hash(userId.ToString() + _config.KeySalt);
        return $"user_{hash}/ {fileName}.jpg";
    }

    private string BuildPublicUrl(string key)
    {
        return $"{_config.PublicUrl}/{_config.BucketName}/{key}";
    }

    private static string Hash(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        var hashBytes = SHA256.HashData(bytes);
        return Convert.ToHexStringLower(hashBytes);
    }
}
