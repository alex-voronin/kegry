namespace VenueModule.API.Dto;

public record GetPresignedUrlResponse(string UploadUrl, string PublicDownloadUrl);