using SharedCore.Enums;

namespace EventModule.API.B2C.Dto;

public record GetPresignedUrlRequest(ContentType ContentType, int ContentLength);
