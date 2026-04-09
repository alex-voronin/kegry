namespace SharedCore.Enums;

public enum ContentType
{
    ImageJpeg,
    ImageWebp
}

public static class SupportedContentTypeExtensions
{
    public static string ToMimeType(this ContentType contentType)
    {
        return contentType switch
        {
            ContentType.ImageJpeg => "image/jpeg",
            ContentType.ImageWebp => "image/webp",
            _ => throw new ArgumentOutOfRangeException(nameof(contentType), contentType, null)
        };
    }

    public static string ToExtension(this ContentType contentType)
    {
        return contentType switch
        {
            ContentType.ImageJpeg => "jpg",
            ContentType.ImageWebp => "webp",
            _ => throw new ArgumentOutOfRangeException(nameof(contentType), contentType, null)
        };
    }
}
