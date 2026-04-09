using SharedCore.Enums;
using SharedCore.ValueObjects;

namespace UserProfileModule.Domain.Services;

public record StorageUrls(string UploadUrl, string PublicDownloadUrl);

public interface IProfileImageService
{
    Task<StorageUrls> GetAvatarUploadUrlAsync(Guid userId, ContentType contentType, int contentLength, CancellationToken ct = default);
    Task<StorageUrls> GetCoverUploadUrlAsync(Guid userId, ContentType contentType, int contentLength, CancellationToken ct = default);
}
