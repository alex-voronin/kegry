using Microsoft.Extensions.Options;
using Shared.Infrastructure.Storage;
using Shared.Infrastructure.Storage.Configs;
using Shouldly;

namespace Tests.Shared.Infrastructure;

public class S3StorageServiceTests
{
    private readonly S3StorageService _sut;
    private readonly S3Config _config;

    public S3StorageServiceTests()
    {
        _config = new S3Config
        {
            ServiceUrl = "https://s3.example.com",
            PublicUrl = "https://cdn.example.com",
            BucketName = "test-bucket",
            AccessKey = "test-key",
            SecretKey = "test-secret",
            KeySalt = "test-salt",
            UseHttp = false
        };

        var options = Options.Create(_config);
        _sut = new S3StorageService(options);
    }

    [Fact]
    public void PublicFileUrlToS3Key_ShouldReturnKey_WhenUrlIsCorrect()
    {
        var key = "folder/image.jpg";
        var publicUrl = $"{_config.PublicUrl}/{key}";
        
        var result = _sut.PublicFileUrlToS3Key(publicUrl);
        result.ShouldBe(key);
    }

    [Fact]
    public void S3KeyToPublicFileUrl_ShouldReturnUrl_WhenKeyIsProvided()
    {
        var key = "folder/image.jpg";
        
        var result = _sut.S3KeyToPublicFileUrl(key);
        result.ShouldBe($"{_config.PublicUrl}/{key}");
    }
}
