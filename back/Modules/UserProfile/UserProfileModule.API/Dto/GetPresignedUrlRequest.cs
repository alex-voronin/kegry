using SharedCore.Enums;

namespace UserProfileModule.API.Dto;

public record GetPresignedUrlRequest(ContentType ContentType, int ContentLength);