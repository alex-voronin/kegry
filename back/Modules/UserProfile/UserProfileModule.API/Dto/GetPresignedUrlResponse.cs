namespace UserProfileModule.API.Dto;

public record GetPresignedUrlResponse(string UploadUrl, string PublicDownloadUrl);