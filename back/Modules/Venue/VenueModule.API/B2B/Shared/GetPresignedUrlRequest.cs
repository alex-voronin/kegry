using SharedCore.Enums;

namespace VenueModule.API.B2B.Shared;

public record GetPresignedUrlRequest(ContentType ContentType, int ContentLength);