using System.Net.Http.Headers;

namespace IntegrationTests.Shared.HttpClients;

public class S3Client
{
    private readonly HttpClient _s3HttpClient = new HttpClient();

    public async Task<HttpResponseMessage> UploadAsync(
        string url,
        byte[] data,
        string contentType,
        CancellationToken ct = default)
    {
        var content = new ByteArrayContent(data);
        content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        return await _s3HttpClient.PutAsync(url, content, ct);
    }

    public async Task<byte[]> Download(string url, CancellationToken ct = default)
    {
        return await _s3HttpClient.GetByteArrayAsync(url, ct);
    }
}