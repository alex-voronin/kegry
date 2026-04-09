using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Shared.Infrastructure.Storage;
using Shared.Infrastructure.Storage.Configs;
using SharedCore.Enums;
using SharedCore.Services.LifeTimes;
using UserProfileModule.Domain.Services;

namespace UserProfileModule.Infrastructure.Storage;

internal class ProfileImageService : IProfileImageService, ISingleton
{
    private const int HashKeyLength = 20;
    private readonly IS3StorageService _storageService;
    private readonly S3Config _config;

    public ProfileImageService(IS3StorageService storageService, IOptions<S3Config> config)
    {
        _storageService = storageService;
        _config = config.Value;
    }

    public async Task<StorageUrls> GetAvatarUploadUrlAsync(Guid userId,
        ContentType contentType,
        int contentLength,
        CancellationToken ct = default)
    {
        var key = GenerateKey(userId, fileName: "avatar", contentType: contentType);
        var presignedUrl = await _storageService.GetPresignedUploadUrlAsync(
            key: key,
            contentType: contentType,
            contentLength: contentLength,
            expires: TimeSpan.FromMinutes(10));
        var publicUrl = _storageService.S3KeyToPublicFileUrl(key, null);
        return new StorageUrls(presignedUrl, publicUrl);
    }

    public async Task<StorageUrls> GetCoverUploadUrlAsync(Guid userId,
        ContentType contentType,
        int contentLength,
        CancellationToken ct = default)
    {
        var key = GenerateKey(userId, fileName: "cover", contentType: contentType);
        var presignedUrl = await _storageService.GetPresignedUploadUrlAsync(
            key: key,
            contentType: contentType,
            contentLength: contentLength,
            expires: TimeSpan.FromMinutes(10));
        var publicUrl = _storageService.S3KeyToPublicFileUrl(key, null);
        return new StorageUrls(presignedUrl, publicUrl);
    }

    private string GenerateKey(Guid userId, string fileName, ContentType contentType)
    {
        var hash = Hash(userId.ToString() + _config.KeySalt);
        var hashPrefix = hash[..HashKeyLength];
        return $"user_{hashPrefix}/{fileName}.{contentType.ToExtension()}";
    }

    private static string Hash(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        var hashBytes = SHA256.HashData(bytes);
        return Convert.ToHexStringLower(hashBytes);
    }
}