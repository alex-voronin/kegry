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

    public async Task<string> GetAvatarUploadUrlAsync(Guid userId, CancellationToken ct = default)
    {
        var key = GenerateKey(userId, fileName: "avatar");
        return await _storageService.GetPresignedUploadUrlAsync(key, "image/jpeg", TimeSpan.FromMinutes(10), ct);
    }

    public async Task<string> GetCoverUploadUrlAsync(Guid userId, CancellationToken ct = default)
    {
        var key = GenerateKey(userId, fileName: "cover");
        return await _storageService.GetPresignedUploadUrlAsync(key, "image/jpeg", TimeSpan.FromMinutes(10), ct);
    }

    private string GenerateKey(Guid userId, string fileName)
    {
        var hash = Hash(userId.ToString() + _config.KeySalt);
        return $"user_{hash}/ {fileName}.jpg";
    }

    private static string Hash(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        var hashBytes = SHA256.HashData(bytes);
        return Convert.ToHexStringLower(hashBytes);
    }
}
