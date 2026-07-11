using EventModule.Contracts.Events;
using EventModule.Domain.Aggregates;
using EventModule.Domain.Enums;
using EventModule.Domain.Exceptions;
using EventModule.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using SharedCore.Services.LifeTimes;

namespace EventModule.Domain.Services;

public interface IEventActionService
{
    Task JoinEventAsync(Guid eventId, Guid userId, CancellationToken ct = default);

    Task LeaveEventAsync(Guid eventId, Guid userId, CancellationToken ct = default);

    Task ApproveParticipantAsync(Guid eventId, Guid currentUserId, Guid targetUserId, CancellationToken ct = default);

    Task RejectParticipantAsync(Guid eventId, Guid currentUserId, Guid targetUserId, CancellationToken ct = default);
}

internal class EventActionService : IEventActionService, IScoped
{
    private readonly IEventRepository _eventRepository;

    public EventActionService(IEventRepository eventRepository)
    {
        _eventRepository = eventRepository;
    }

    public async Task JoinEventAsync(Guid eventId, Guid userId, CancellationToken ct = default)
    {
        await SaveWithRetryAsync(async () =>
        {
            var ev = await LoadEventWithParticipantsAsync(eventId);

            if (ev.EventParticipants.Any(p => p.UserId == userId))
                throw EventExceptions.AlreadyParticipant();

            if (ev.MaxParticipants.HasValue &&
                ev.EventParticipants.Count(p => p.Status == ParticipantStatus.Approved) >= ev.MaxParticipants.Value)
                throw EventExceptions.EventFull();

            var status = ev.RequiresApproval ? ParticipantStatus.Pending : ParticipantStatus.Approved;
            var participant = new EventParticipant(eventId, userId, DateTimeOffset.UtcNow, status);
            ev.AddParticipant(participant);
            ev.UpdatedAt = DateTimeOffset.UtcNow;

            if (status == ParticipantStatus.Pending)
            {
                await _eventRepository.PublishEventAsync(
                    new UserRequestedApproveEvent()
                    {
                        EventCreatorId = ev.CreatorId,
                        EventId = eventId,
                        EventTitle = ev.Title,
                        EventStartDate = ev.StartDateTime,
                        JoinedUserId = userId,
                    }
                );
            }

            await _eventRepository.SaveChangesAndFlushMessagesAsync(ct);
        });
    }

    public async Task LeaveEventAsync(Guid eventId, Guid userId, CancellationToken ct = default)
    {
        await SaveWithRetryAsync(async () =>
        {
            var ev = await LoadEventWithParticipantsAsync(eventId);

            if (ev.CreatorId == userId)
                throw EventExceptions.CreatorCannotLeave();

            if (!ev.EventParticipants.Any(p => p.UserId == userId))
                throw EventExceptions.NotParticipant();

            ev.RemoveParticipant(userId);
            ev.UpdatedAt = DateTimeOffset.UtcNow;

            await _eventRepository.SaveChangesAsync(ct);
        });
    }

    public async Task ApproveParticipantAsync(Guid eventId, Guid currentUserId, Guid targetUserId,
        CancellationToken ct = default)
    {
        await SaveWithRetryAsync(async () =>
        {
            var ev = await LoadEventWithParticipantsAsync(eventId);

            if (ev.CreatorId != currentUserId)
                throw EventExceptions.NotEventCreator();
            if (ev.Type != EventType.Meetup)
                throw EventExceptions.ApprovalNotSupported();

            var participant = ev.GetParticipant(targetUserId)
                              ?? throw EventExceptions.NotParticipant();

            participant.Status = ParticipantStatus.Approved;
            participant.ApprovedAt = DateTimeOffset.UtcNow;
            ev.UpdatedAt = DateTimeOffset.UtcNow;

            await _eventRepository.PublishEventAsync(
                new UserParticipationApprovedEvent()
                {
                    EventId = eventId,
                    EventTitle = ev.Title,
                    EventStartDate = ev.StartDateTime,
                    EventCreatorId = ev.CreatorId,
                    JoinedUserId = targetUserId
                }
            );

            await _eventRepository.SaveChangesAndFlushMessagesAsync(ct);
        });
    }

    public async Task RejectParticipantAsync(Guid eventId, Guid currentUserId, Guid targetUserId,
        CancellationToken ct = default)
    {
        await SaveWithRetryAsync(async () =>
        {
            var ev = await LoadEventWithParticipantsAsync(eventId);

            if (ev.CreatorId != currentUserId)
                throw EventExceptions.NotEventCreator();
            if (ev.Type != EventType.Meetup)
                throw EventExceptions.ApprovalNotSupported();

            var participant = ev.GetParticipant(targetUserId)
                              ?? throw EventExceptions.NotParticipant();

            participant.Status = ParticipantStatus.Rejected;
            participant.ApprovedAt = null;
            ev.UpdatedAt = DateTimeOffset.UtcNow;

            await _eventRepository.SaveChangesAsync(ct);
        });
    }

    private async Task<Event> LoadEventWithParticipantsAsync(Guid eventId)
    {
        return await _eventRepository.GetByIdWithRelationsAsync(eventId)
               ?? throw EventExceptions.EventNotFound(eventId);
    }

    private static async Task SaveWithRetryAsync(Func<Task> action, int maxRetries = 3)
    {
        for (var attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                await action();
                return;
            }
            catch (DbUpdateConcurrencyException) when (attempt < maxRetries)
            {
            }
        }

        // Last attempt — let it throw naturally
        await action();
    }
}