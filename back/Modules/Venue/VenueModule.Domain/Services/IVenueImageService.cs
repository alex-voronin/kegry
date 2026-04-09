using SharedCore.Enums;

namespace VenueModule.Domain.Services;

public record StorageUrls(string UploadUrl, string PublicDownloadUrl);

public interface IVenueImageService
{
    Task<StorageUrls> GetCompanyAvatarUploadUrlAsync(Guid companyId,
        ContentType contentType,
        int contentLength,
        CancellationToken ct = default);

    Task<StorageUrls> GetVenueCoverUploadUrlAsync(Guid companyId,
        Guid venueId,
        ContentType contentType,
        int contentLength,
        CancellationToken ct = default);
}