using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using SharedCore.Services.LifeTimes;
using UserProfileModule.Infrastructure.Storage.Configs;

namespace UserProfileModule.Infrastructure.Storage;

public interface IStorageService
{
    Task<string> GetPresignedUploadUrlAsync(
        string key,
        string contentType,
        TimeSpan expires,
        CancellationToken ct = default);
}

internal class S3StorageService : IStorageService, ISingleton
{
    private readonly S3Config _config;
    private readonly IAmazonS3 _s3Client;

    public S3StorageService(IOptions<S3Config> config)
    {
        _config = config.Value;
        
        var s3Config = new AmazonS3Config
        {
            ServiceURL = _config.ServiceUrl,
            ForcePathStyle = true // Required for R2
        };

        _s3Client = new AmazonS3Client(_config.AccessKey, _config.SecretKey, s3Config);
    }

    public async Task<string> GetPresignedUploadUrlAsync(
        string key,
        string contentType,
        TimeSpan expires,
        CancellationToken ct = default)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = _config.BucketName,
            Key = key,
            ContentType = contentType,
            Verb = HttpVerb.PUT,
            Expires = DateTime.UtcNow.Add(expires)
        };

        return await Task.Run(() => _s3Client.GetPreSignedURL(request), ct);
    }
}
