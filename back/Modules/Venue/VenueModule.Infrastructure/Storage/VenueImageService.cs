using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Shared.Infrastructure.Storage;
using Shared.Infrastructure.Storage.Configs;
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

    public async Task<StorageUrls> GetCompanyAvatarUploadUrlAsync(Guid companyId, int contentLength,
        CancellationToken ct = default)
    {
        var key = GenerateAvatarKey(companyId, fileName: "avatar");
        var presignedUrl = await _storageService.GetPresignedUploadUrlAsync(
            key: key,
            contentType: "image/jpeg",
            contentLength: contentLength,
            expires: TimeSpan.FromMinutes(10));
        var publicUrl = _storageService.S3KeyToPublicFileUrl(key, null);
        return new StorageUrls(presignedUrl, publicUrl);
    }

    public async Task<StorageUrls> GetVenueCoverUploadUrlAsync(Guid companyId, Guid venueId, int contentLength,
        CancellationToken ct = default)
    {
        var key = GenerateCoverKey(companyId, venueId, fileName: "cover");
        var presignedUrl = await _storageService.GetPresignedUploadUrlAsync(
            key: key,
            contentType: "image/jpeg",
            contentLength: contentLength,
            expires: TimeSpan.FromMinutes(10));
        var publicUrl = _storageService.S3KeyToPublicFileUrl(key, null);
        return new StorageUrls(presignedUrl, publicUrl);
    }

    private string GenerateAvatarKey(Guid companyId, string fileName)
    {
        var hash = Hash(companyId.ToString() + _config.KeySalt);
        var hashPrefix = hash[..HashKeyLength];
        return $"company_{hashPrefix}/{fileName}.jpg";
    }

    private string GenerateCoverKey(Guid companyId, Guid venuId, string fileName)
    {
        var companyHash = Hash(companyId.ToString() + _config.KeySalt);
        var venueHash = Hash(venuId.ToString() + _config.KeySalt);
        var companyPrefix = companyHash[..HashKeyLength];
        var venuePrefix   = venueHash[..HashKeyLength];
        return $"company_{companyPrefix}/venue_{venuePrefix}/{fileName}.jpg";
    }

    private static string Hash(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        var hashBytes = SHA256.HashData(bytes);
        return Convert.ToHexStringLower(hashBytes);
    }
}