using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Shared.Infrastructure.Storage;
using Shared.Infrastructure.Storage.Configs;
using SharedCore.Enums;
using SharedCore.Services.LifeTimes;
using EventModule.Domain.Services;

namespace EventModule.Infrastructure.Storage;

internal class EventImageService : IEventImageService, ISingleton
{
    private const int HashKeyLength = 20;

    private readonly IS3StorageService _storageService;
    private readonly S3Config _config;

    public EventImageService(IS3StorageService storageService, IOptions<S3Config> config)
    {
        _storageService = storageService;
        _config = config.Value;
    }

    public async Task<StorageUrls> GetEventCoverUploadUrlAsync(
        Guid eventId,
        ContentType contentType,
        int contentLength,
        CancellationToken ct = default)
    {
        var key = GenerateCoverKey(eventId, "cover", contentType);
        var presignedUrl = await _storageService.GetPresignedUploadUrlAsync(
            key: key,
            contentType: contentType,
            contentLength: contentLength,
            expires: TimeSpan.FromMinutes(10));
        var publicUrl = _storageService.S3KeyToPublicFileUrl(key, null);
        return new StorageUrls(presignedUrl, publicUrl);
    }

    private string GenerateCoverKey(Guid eventId, string fileName, ContentType contentType)
    {
        var hash = Hash(eventId.ToString() + _config.KeySalt);
        var hashPrefix = hash[..HashKeyLength];
        return $"event_{hashPrefix}/{fileName}.{contentType.ToExtension()}";
    }

    private static string Hash(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        var hashBytes = SHA256.HashData(bytes);
        return Convert.ToHexStringLower(hashBytes);
    }
}
