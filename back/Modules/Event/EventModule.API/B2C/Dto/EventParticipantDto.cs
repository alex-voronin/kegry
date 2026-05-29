using EventModule.Domain.Enums;

namespace EventModule.API.B2C.Dto;

public record EventParticipantDto(
    string UserId,
    string? FirstName,
    string? LastName,
    string? AvatarUrl,
    DateTimeOffset JoinedAt,
    DateTimeOffset? ApprovedAt,
    ParticipantStatus Status);
