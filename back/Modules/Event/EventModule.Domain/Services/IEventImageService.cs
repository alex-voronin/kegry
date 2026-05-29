using SharedCore.Enums;

namespace EventModule.Domain.Services;

public record StorageUrls(string UploadUrl, string PublicDownloadUrl);

public interface IEventImageService
{
    Task<StorageUrls> GetEventCoverUploadUrlAsync(
        Guid eventId,
        ContentType contentType,
        int contentLength,
        CancellationToken ct = default);
}
