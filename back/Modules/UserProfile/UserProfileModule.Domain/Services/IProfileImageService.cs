namespace UserProfileModule.Domain.Services;

public record StorageUrls(string UploadUrl, string PublicDownloadUrl);

public interface IProfileImageService
{
    Task<StorageUrls> GetAvatarUploadUrlAsync(Guid userId, int contentLength, CancellationToken ct = default);
    Task<StorageUrls> GetCoverUploadUrlAsync(Guid userId, int contentLength, CancellationToken ct = default);
}
