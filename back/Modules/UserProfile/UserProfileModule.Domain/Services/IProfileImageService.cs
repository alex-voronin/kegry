namespace UserProfileModule.Domain.Services;

public interface IProfileImageService
{
    Task<string> GetAvatarUploadUrlAsync(Guid userId, CancellationToken ct = default);
    Task<string> GetCoverUploadUrlAsync(Guid userId, CancellationToken ct = default);
}
