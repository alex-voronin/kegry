using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Shared.Infrastructure.Storage;
using Shared.Infrastructure.Storage.Configs;
using SharedCore.Services.LifeTimes;
using UserProfileModule.Domain.Services;

namespace UserProfileModule.Infrastructure.Storage;

internal class ProfileImageService : IProfileImageService, ISingleton
{
    private readonly IS3StorageService _storageService;
    private readonly S3Config _config;

    public ProfileImageService(IS3StorageService storageService, IOptions<S3Config> config)
    {
        _storageService = storageService;
        _config = config.Value;
    }

    public async Task<StorageUrls> GetAvatarUploadUrlAsync(Guid userId, int contentLength,
        CancellationToken ct = default)
    {
        var key = GenerateKey(userId, fileName: "avatar");
        var presignedUrl = await _storageService.GetPresignedUploadUrlAsync(
            key: key,
            contentType: "image/jpeg",
            contentLength: contentLength,
            expires: TimeSpan.FromMinutes(10));
        var publicUrl = _storageService.S3KeyToPublicFileUrl(key, null);
        return new StorageUrls(presignedUrl, publicUrl);
    }

    public async Task<StorageUrls> GetCoverUploadUrlAsync(Guid userId, int contentLength,
        CancellationToken ct = default)
    {
        var key = GenerateKey(userId, fileName: "cover");
        var presignedUrl = await _storageService.GetPresignedUploadUrlAsync(
            key: key,
            contentType: "image/jpeg",
            contentLength: contentLength,
            expires: TimeSpan.FromMinutes(10));
        var publicUrl = _storageService.S3KeyToPublicFileUrl(key, null);
        return new StorageUrls(presignedUrl, publicUrl);
    }

    private string GenerateKey(Guid userId, string fileName)
    {
        var hash = Hash(userId.ToString() + _config.KeySalt);
        return $"user_{hash}/{fileName}.jpg";
    }

    private static string Hash(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        var hashBytes = SHA256.HashData(bytes);
        return Convert.ToHexStringLower(hashBytes);
    }
}