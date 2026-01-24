using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using Shared.Infrastructure.Storage.Configs;
using SharedCore.Services.LifeTimes;

namespace Shared.Infrastructure.Storage;

public interface IS3StorageService
{
    public string PublicFileUrlToS3Key(string publicFileUrl);
    public string S3KeyToPublicFileUrl(string s3Key);

    public Task<string> GetPresignedUploadUrlAsync(
        string key,
        string contentType,
        int contentLength,
        TimeSpan expires);
}

internal class S3StorageService : IS3StorageService, ISingleton
{
    private readonly S3Config _config;
    private readonly IAmazonS3 _s3Client;

    public S3StorageService(IOptions<S3Config> config)
    {
        _config = config.Value;

        var s3Config = new AmazonS3Config
        {
            ServiceURL = _config.ServiceUrl,
            UseHttp = _config.UseHttp,
            ForcePathStyle = true // Required for R2
        };

        _s3Client = new AmazonS3Client(_config.AccessKey, _config.SecretKey, s3Config);
    }

    public string PublicFileUrlToS3Key(string publicFileUrl)
    {
        return publicFileUrl.Substring(_config.PublicUrl.Length + 1);
    }

    public string S3KeyToPublicFileUrl(string s3Key)
    {
        return $"{_config.PublicUrl}/{s3Key}";
    }

    public Task<string> GetPresignedUploadUrlAsync(
        string key,
        string contentType,
        int contentLength,
        TimeSpan expires)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = _config.BucketName,
            Key = key,
            ContentType = contentType,
            Verb = HttpVerb.PUT,
            Expires = DateTime.UtcNow.Add(expires)
        };
        
        request.Headers["Content-Length"] = contentLength.ToString();

        if (_s3Client.Config.UseHttp)
        {
            request.Protocol = Protocol.HTTP;
        }

        return _s3Client.GetPreSignedURLAsync(request);
    }
}