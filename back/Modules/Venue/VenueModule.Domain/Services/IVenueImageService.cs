namespace VenueModule.Domain.Services;

public record StorageUrls(string UploadUrl, string PublicDownloadUrl);

public interface IVenueImageService
{
    Task<StorageUrls> GetCompanyAvatarUploadUrlAsync(Guid companyId, int contentLength, CancellationToken ct = default);

    Task<StorageUrls> GetVenueCoverUploadUrlAsync(Guid companyId, Guid venueId, int contentLength,
        CancellationToken ct = default);
}