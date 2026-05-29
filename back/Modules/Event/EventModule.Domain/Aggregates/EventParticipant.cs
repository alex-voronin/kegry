using EventModule.Domain.Enums;

namespace EventModule.Domain.Aggregates;

public class EventParticipant
{
    public Guid EventId { get; init; }
    public Guid UserId { get; init; }
    public DateTimeOffset JoinedAt { get; init; }
    public DateTimeOffset? ApprovedAt { get; set; }
    public ParticipantStatus Status { get; set; }

    internal EventParticipant()
    {
    }

    public EventParticipant(Guid eventId, Guid userId, DateTimeOffset joinedAt, ParticipantStatus status)
    {
        EventId = eventId;
        UserId = userId;
        JoinedAt = joinedAt;
        Status = status;
    }
}
