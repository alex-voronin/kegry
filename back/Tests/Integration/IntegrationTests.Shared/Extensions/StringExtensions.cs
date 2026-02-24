using Shouldly;

namespace IntegrationTests.Shared.Extensions;

public static class StringExtensions
{
    public static void ShouldBeNewerOrTheSameS3Url(this string s3Url, string otherS3Url)
    {
        var (thisUrl, thisTimestamp) = SplitToUrlAndTimestamp(s3Url);
        var (otherUrl, otherTimestamp) = SplitToUrlAndTimestamp(otherS3Url);
        thisUrl.ShouldBe(otherUrl);
        if (otherTimestamp == null)
        {
            thisTimestamp.ShouldNotBeNull();
            otherTimestamp.ShouldBeNull();
        }
        else
        {
            thisTimestamp.ShouldNotBeNull();
            otherTimestamp.Value.ShouldBeLessThanOrEqualTo(thisTimestamp.Value);
        }
    }

    private static (string url, int? timestamp) SplitToUrlAndTimestamp(string url)
    {
        var parts = url.Split("?v=");
        return parts.Length switch
        {
            1 => (parts[0], null),
            2 => (parts[0], int.Parse(parts[1])),
            _ => throw new ArgumentException($"Invalid S3 url: {url}")
        };
    }
}