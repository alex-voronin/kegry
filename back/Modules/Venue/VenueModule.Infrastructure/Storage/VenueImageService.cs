using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Shared.Infrastructure.Storage;
using Shared.Infrastructure.Storage.Configs;
using SharedCore.Enums;
using SharedCore.Services.LifeTimes;
using VenueModule.Domain.Services;

namespace VenueModule.Infrastructure.Storage;

internal class VenueImageService : IVenueImageService, ISingleton
{
    private const int HashKeyLength = 20;

    private readonly IS3StorageService _storageService;
    private readonly S3Config _config;

    public VenueImageService(IS3StorageService storageService, IOptions<S3Config> config)
    {
        _storageService = storageService;
        _config = config.Value;
    }

    public async Task<StorageUrls> GetCompanyAvatarUploadUrlAsync(Guid companyId,
        ContentType contentType,
        int contentLength,
        CancellationToken ct = default)
    {
        var key = GenerateAvatarKey(companyId, fileName: "avatar", contentType: contentType);
        var presignedUrl = await _storageService.GetPresignedUploadUrlAsync(
            key: key,
            contentType: contentType,
            contentLength: contentLength,
            expires: TimeSpan.FromMinutes(10));
        var publicUrl = _storageService.S3KeyToPublicFileUrl(key, null);
        return new StorageUrls(presignedUrl, publicUrl);
    }

    public async Task<StorageUrls> GetVenueCoverUploadUrlAsync(Guid companyId,
        Guid venueId,
        ContentType contentType,
        int contentLength,
        CancellationToken ct = default)
    {
        var key = GenerateCoverKey(companyId, venueId, fileName: "cover", contentType: contentType);
        var presignedUrl = await _storageService.GetPresignedUploadUrlAsync(
            key: key,
            contentType: contentType,
            contentLength: contentLength,
            expires: TimeSpan.FromMinutes(10));
        var publicUrl = _storageService.S3KeyToPublicFileUrl(key, null);
        return new StorageUrls(presignedUrl, publicUrl);
    }

    private string GenerateAvatarKey(Guid companyId, string fileName, ContentType contentType)
    {
        var hash = Hash(companyId.ToString() + _config.KeySalt);
        var hashPrefix = hash[..HashKeyLength];
        return $"company_{hashPrefix}/{fileName}.{contentType.ToExtension()}";
    }

    private string GenerateCoverKey(Guid companyId, Guid venuId, string fileName, ContentType contentType)
    {
        var companyHash = Hash(companyId.ToString() + _config.KeySalt);
        var venueHash = Hash(venuId.ToString() + _config.KeySalt);
        var companyPrefix = companyHash[..HashKeyLength];
        var venuePrefix = venueHash[..HashKeyLength];
        return $"company_{companyPrefix}/venue_{venuePrefix}/{fileName}.{contentType.ToExtension()}";
    }

    private static string Hash(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        var hashBytes = SHA256.HashData(bytes);
        return Convert.ToHexStringLower(hashBytes);
    }
}