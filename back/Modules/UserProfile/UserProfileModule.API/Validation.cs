using UserProfileModule.API.Dto;

namespace UserProfileModule.API;

using CoreValidation = SharedCore.Validation.Validation;

public static class Validation
{
    public const int MaxAvatarSizeBytes = 512 * 1024;
    public const int MaxCoverSizeBytes = 1024 * 1024;

    public static void ThrowIfFileSizeExceed(int contentLength, int limit)
    {
        CoreValidation.ThrowIfInvalid(
            nameof(GetPresignedUrlRequest.ContentLength),
            isValid: () => contentLength <= limit,
            validationError: "Upload size is too large");
    }
}