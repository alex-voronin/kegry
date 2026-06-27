using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Storage;
using SharedCore.Api;
using SharedCore.Api.Dto;
using SharedCore.Extensions;
using EventModule.API.B2C.Dto;
using EventModule.Domain.Aggregates;
using EventModule.Domain.Enums;
using EventModule.Domain.Exceptions;
using EventModule.Domain.Repositories;
using EventModule.Domain.Services;
using EventModule.Infrastructure.Persistence;

namespace EventModule.API.B2C;

public class B2CEventEndpoints : IEndpoints
{
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var usersApiGroup = app.MapGroup("")
            .WithTags("Event User API")
            .RequireAuthorization();

        usersApiGroup.MapPost("/api/v1/events/map", GetEventsMap);
        usersApiGroup.MapGet("/api/v1/venues/{venueId:guid}/events", GetVenueEvents);
        usersApiGroup.MapGet("/api/v1/events/{id:guid}", GetEvent);
        usersApiGroup.MapPost("/api/v1/events", CreateEvent);
        usersApiGroup.MapPatch("/api/v1/events/{id:guid}", UpdateEvent);
        usersApiGroup.MapDelete("/api/v1/events/{id:guid}", DeleteEvent);
        usersApiGroup.MapPost("/api/v1/events/{id:guid}/cover/upload-url", GetCoverUploadUrl);
        usersApiGroup.MapPost("/api/v1/events/{id:guid}/join", JoinEvent);
        usersApiGroup.MapPost("/api/v1/events/{id:guid}/leave", LeaveEvent);
        usersApiGroup.MapGet("/api/v1/events/{id:guid}/participants", GetParticipants);
        usersApiGroup.MapPatch("/api/v1/events/{id:guid}/participants/{userId:guid}/approve", ApproveParticipant);
        usersApiGroup.MapPatch("/api/v1/events/{id:guid}/participants/{userId:guid}/reject", RejectParticipant);
        usersApiGroup.MapGet("/api/v1/events/my/created", GetMyCreatedEvents);
        usersApiGroup.MapGet("/api/v1/events/my/participated", GetMyParticipatedEvents);
    }

    private static async Task<ApiResult<VenueEventsDto[]>> GetEventsMap(
        [FromBody] GetEventsMapRequest request,
        ClaimsPrincipal user,
        IEventRepository eventRepository,
        IS3StorageService storageService,
        CancellationToken ct)
    {
        var userId = user.GetId();
        var result = await eventRepository.GetEventsInBoundsAsync(
            request.MinLat, request.MaxLat,
            request.MinLng, request.MaxLng,
            request.EventTypes, request.DateFrom, request.DateTo,
            request.VenueIds, 20, userId, ct);

        EventMapItemDto MapEvent(EventWithParticipantStatus ewps)
        {
            return new EventMapItemDto(
                Id: ewps.Event.Id.ToString(),
                CoverImageUrl: ewps.Event.CoverImageS3Key != null
                    ? storageService.S3KeyToPublicFileUrl(ewps.Event.CoverImageS3Key, ewps.Event.UpdatedAt)
                    : null,
                Title: ewps.Event.Title,
                Type: ewps.Event.Type,
                StartDateTime: ewps.Event.StartDateTime,
                VenueId: ewps.Event.VenueId.ToString(),
                ParticipationStatus: ewps.Status?.ToString(),
                CreatorId: ewps.Event.CreatorId.ToString(),
                CreatorAvatarUrl: ewps.Event.CreatorAvatarS3Key != null
                    ? storageService.S3KeyToPublicFileUrl(ewps.Event.CreatorAvatarS3Key, null)
                    : null);
        }

        var dtos = result.Select(r => new VenueEventsDto(
            VenueId: r.VenueId.ToString(),
            TotalCount: r.TotalCount,
            Events: r.Events.Select(MapEvent).ToArray()
        )).ToArray();

        return ApiResult<VenueEventsDto[]>.Build(dtos);
    }

    private static async Task<ApiResult<EventDto>> GetEvent(
        [FromRoute] Guid id,
        ClaimsPrincipal user,
        IEventRepository eventRepository,
        IS3StorageService storageService,
        [FromQuery] int participantsLimit = 50,
        CancellationToken ct = default)
    {
        var ev = await eventRepository.GetByIdWithRelationsAsync(id, ct);
        if (ev == null)
            throw EventExceptions.EventNotFound(id);

        var userId = user.GetId();
        var isCreator = ev.CreatorId == userId;
        var participationStatus = ev.EventParticipants
            .FirstOrDefault(p => p.UserId == userId)?.Status;

        var contacts = ev.EventContacts
            .Where(c => c.IsPublic || isCreator || participationStatus == ParticipantStatus.Approved)
            .Select(c => new EventContactDto(c.Id, c.Type, c.Value, c.IsPublic))
            .ToArray();

        var dto = new EventDto(
            Id: ev.Id.ToString(),
            CoverImageUrl: ev.CoverImageS3Key != null
                ? storageService.S3KeyToPublicFileUrl(ev.CoverImageS3Key, ev.UpdatedAt)
                : null,
            Title: ev.Title,
            Description: ev.Description,
            StartDateTime: ev.StartDateTime,
            Type: ev.Type,
            VenueId: ev.VenueId.ToString(),
            CreatorId: ev.CreatorId.ToString(),
            CreatorAvatarUrl: ev.CreatorAvatarS3Key != null
                ? storageService.S3KeyToPublicFileUrl(ev.CreatorAvatarS3Key, ev.UpdatedAt)
                : null,
            IsVisibleOnMap: ev.IsVisibleOnMap,
            MaxParticipants: ev.MaxParticipants,
            RequiresApproval: ev.RequiresApproval,
            ParticipantsCount: ev.EventParticipants.Count(p => p.Status == ParticipantStatus.Approved),
            PendingApprovalsCount: ev.EventParticipants.Count(p => p.Status == ParticipantStatus.Pending),
            PendingParticipantIds: isCreator
                ? ev.EventParticipants.Where(p => p.Status == ParticipantStatus.Pending).Select(p => p.UserId).Take(participantsLimit).ToArray()
                : [],
            ApprovedParticipantIds: isCreator
                ? ev.EventParticipants.Where(p => p.Status == ParticipantStatus.Approved).Select(p => p.UserId).Take(participantsLimit).ToArray()
                : [],
            Contacts: contacts,
            HobbyIds: ev.EventHobbyLinks.Select(h => h.HobbyId).ToArray(),
            ParticipationStatus: participationStatus?.ToString(),
            IsCreator: isCreator);

        return ApiResult<EventDto>.Build(dto);
    }

    private static async Task<ApiResult<PaginatedResponse<EventListItemDto>>> GetVenueEvents(
        [AsParameters] GetVenueEventsRequest request,
        ClaimsPrincipal user,
        IEventRepository eventRepository,
        IS3StorageService storageService,
        CancellationToken ct)
    {
        var userId = user.GetId();
        var (events, totalCount) = await eventRepository.GetVenueEventsAsync(
            request.VenueId, request.Offset, request.Limit, ct);

        var dtos = events.Select(e =>
        {
            return new EventListItemDto(
                Id: e.Id.ToString(),
                VenueId: e.VenueId.ToString(),
                CoverImageUrl: e.CoverImageS3Key != null
                    ? storageService.S3KeyToPublicFileUrl(e.CoverImageS3Key, e.UpdatedAt)
                    : null,
                Title: e.Title,
                Type: e.Type,
                StartDateTime: e.StartDateTime,
                ParticipantsCount: e.EventParticipants.Count(p => p.Status == ParticipantStatus.Approved),
                ParticipationStatus: e.EventParticipants.Where(p => p.UserId == userId).Select(p => (ParticipantStatus?)p.Status).FirstOrDefault()?.ToString(),
                CreatorId: e.CreatorId.ToString(),
                CreatorAvatarUrl: e.CreatorAvatarS3Key != null
                    ? storageService.S3KeyToPublicFileUrl(e.CreatorAvatarS3Key, null)
                    : null);
        }).ToArray();

        return ApiResult<PaginatedResponse<EventListItemDto>>.Build(
            new PaginatedResponse<EventListItemDto>(dtos, totalCount));
    }

    private static async Task<ApiResult<EventDto>> CreateEvent(
        [FromBody] CreateEventRequest request,
        ClaimsPrincipal user,
        IEventRepository eventRepository,
        IS3StorageService storageService,
        CancellationToken ct)
    {
        var userId = user.GetId();
        var eventId = Guid.CreateVersion7();

        var ev = new Event(
            id: eventId,
            coverImageS3Key: request.CoverImageUrl != null
                ? storageService.PublicFileUrlToS3Key(request.CoverImageUrl)
                : null,
            title: request.Title,
            description: request.Description,
            startDateTime: request.StartDateTime,
            type: EventType.Meetup,
            venueId: request.VenueId,
            creatorId: userId,
            creatorAvatarS3Key: request.CreatorAvatarUrl != null ? storageService.PublicFileUrlToS3Key(request.CreatorAvatarUrl) : null,
            maxParticipants: request.MaxParticipants,
            requiresApproval: request.RequiresApproval);

        if (request.Contacts is { Length: > 0 })
        {
            ev.SetContacts(request.Contacts.Select(c =>
                new EventContact(Guid.CreateVersion7(), eventId, c.Type, c.Value, c.IsPublic)).ToArray());
        }

        if (request.HobbyIds is { Length: > 0 })
        {
            ev.SetHobbyLinks(request.HobbyIds.Select(h => new EventHobbyLink(eventId, h)).ToArray());
        }

        ev.AddParticipant(new EventParticipant(
            eventId: eventId,
            userId: userId,
            joinedAt: DateTimeOffset.UtcNow,
            status: ParticipantStatus.Approved)
        { ApprovedAt = DateTimeOffset.UtcNow });

        await eventRepository.AddAsync(ev, ct);
        await eventRepository.SaveChangesAsync(ct);

        return await GetEvent(eventId, user, eventRepository, storageService, ct: ct);
    }

    private static async Task<ApiResult<EventDto>> UpdateEvent(
        [FromRoute] Guid id,
        [FromBody] UpdateEventRequest request,
        ClaimsPrincipal user,
        IEventRepository eventRepository,
        EventContext dbContext,
        IS3StorageService storageService,
        CancellationToken ct)
    {
        var ev = await eventRepository.GetByIdWithRelationsAsync(id, ct);
        if (ev == null)
            throw EventExceptions.EventNotFound(id);
        if (ev.CreatorId != user.GetId())
            throw EventExceptions.NotEventCreator();

        ev.UpdateDetails(
            coverImageS3Key: request.CoverImageUrl != null
                ? storageService.PublicFileUrlToS3Key(request.CoverImageUrl)
                : ev.CoverImageS3Key,
            title: request.Title ?? ev.Title,
            description: request.Description ?? ev.Description,
            startDateTime: request.StartDateTime ?? ev.StartDateTime,
            isVisibleOnMap: request.IsVisibleOnMap ?? ev.IsVisibleOnMap,
            maxParticipants: request.MaxParticipants ?? ev.MaxParticipants,
            requiresApproval: request.RequiresApproval ?? ev.RequiresApproval);

        if (request.Contacts != null)
        {
            ev.SetContacts(request.Contacts.Select(c =>
                new EventContact(Guid.CreateVersion7(), id, c.Type, c.Value, c.IsPublic)).ToArray());
        }

        if (request.HobbyIds != null)
        {
            ev.SetHobbyLinks(request.HobbyIds.Select(h => new EventHobbyLink(id, h)).ToArray());
        }

        await eventRepository.SaveChangesAsync(ct);

        return await GetEvent(id, user, eventRepository, storageService, ct: ct);
    }

    private static async Task<ApiResult.EmptyResult> DeleteEvent(
        [FromRoute] Guid id,
        ClaimsPrincipal user,
        IEventRepository eventRepository,
        IS3StorageService storageService,
        CancellationToken ct)
    {
        var ev = await eventRepository.GetByIdAsync(id, ct: ct);
        if (ev == null)
            throw EventExceptions.EventNotFound(id);
        if (ev.CreatorId != user.GetId())
            throw EventExceptions.NotEventCreator();

        var coverKey = ev.CoverImageS3Key;

        ev.IsDeleted = true;
        await eventRepository.SaveChangesAsync(ct);

        if (coverKey != null)
            await storageService.DeleteObjectAsync(coverKey, dontThrow: true);

        return ApiResult.Empty;
    }

    private static async Task<ApiResult<EventParticipantDto[]>> GetParticipants(
        [FromRoute] Guid id,
        ClaimsPrincipal user,
        IEventRepository eventRepository,
        EventContext dbContext,
        IS3StorageService storageService,
        CancellationToken ct)
    {
        var ev = await eventRepository.GetByIdWithRelationsAsync(id, ct);
        if (ev == null)
            throw EventExceptions.EventNotFound(id);
        if (ev.CreatorId != user.GetId())
            throw EventExceptions.NotEventCreator();

        var userIds = ev.EventParticipants.Select(p => p.UserId).ToArray();
        var users = await dbContext.Users
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, ct);

        var dtos = ev.EventParticipants.Select(p =>
        {
            var u = users.GetValueOrDefault(p.UserId);
            return new EventParticipantDto(
                UserId: p.UserId.ToString(),
                FirstName: u?.FirstName,
                LastName: u?.LastName,
                AvatarUrl: u?.AvatarImageS3Key != null
                    ? storageService.S3KeyToPublicFileUrl(u.AvatarImageS3Key, null)
                    : null,
                JoinedAt: p.JoinedAt,
                ApprovedAt: p.ApprovedAt,
                Status: p.Status);
        }).ToArray();

        return ApiResult<EventParticipantDto[]>.Build(dtos);
    }

    private static async Task<ApiResult.EmptyResult> JoinEvent(
        [FromRoute] Guid id,
        ClaimsPrincipal user,
        IEventActionService eventActionService,
        CancellationToken ct)
    {
        await eventActionService.JoinEventAsync(id, user.GetId());
        return ApiResult.Empty;
    }

    private static async Task<ApiResult.EmptyResult> LeaveEvent(
        [FromRoute] Guid id,
        ClaimsPrincipal user,
        IEventActionService eventActionService,
        CancellationToken ct)
    {
        await eventActionService.LeaveEventAsync(id, user.GetId());
        return ApiResult.Empty;
    }

    private static async Task<ApiResult.EmptyResult> ApproveParticipant(
        [FromRoute] Guid id,
        [FromRoute] Guid userId,
        ClaimsPrincipal user,
        IEventActionService eventActionService,
        CancellationToken ct)
    {
        await eventActionService.ApproveParticipantAsync(id, user.GetId(), userId, ct);
        return ApiResult.Empty;
    }

    private static async Task<ApiResult.EmptyResult> RejectParticipant(
        [FromRoute] Guid id,
        [FromRoute] Guid userId,
        ClaimsPrincipal user,
        IEventActionService eventActionService,
        CancellationToken ct)
    {
        await eventActionService.RejectParticipantAsync(id, user.GetId(), userId);
        return ApiResult.Empty;
    }

    private static async Task<ApiResult<GetPresignedUrlResponse>> GetCoverUploadUrl(
        [FromRoute] Guid id,
        [FromBody] GetPresignedUrlRequest request,
        ClaimsPrincipal user,
        IEventRepository eventRepository,
        IEventImageService imageService,
        CancellationToken ct)
    {
        var ev = await eventRepository.GetByIdAsync(id, ct: ct);
        if (ev == null)
            throw EventExceptions.EventNotFound(id);
        if (ev.CreatorId != user.GetId())
            throw EventExceptions.NotEventCreator();

        var urls = await imageService.GetEventCoverUploadUrlAsync(
            eventId: id,
            contentType: request.ContentType,
            contentLength: request.ContentLength,
            ct: ct);

        return ApiResult<GetPresignedUrlResponse>.Build(
            new GetPresignedUrlResponse(UploadUrl: urls.UploadUrl, PublicDownloadUrl: urls.PublicDownloadUrl));
    }

    private static async Task<ApiResult<PaginatedResponse<EventListItemDto>>> GetMyCreatedEvents(
        [AsParameters] PaginatedRequest request,
        ClaimsPrincipal user,
        IEventRepository eventRepository,
        IS3StorageService storageService,
        CancellationToken ct)
    {
        var userId = user.GetId();
        var (events, totalCount) = await eventRepository.GetOwnEventsPaginatedAsync(
            userId, offset: request.Offset, limit: request.Limit, ct: ct);

        var dtos = MapToEventListItemDtos(events, userId, storageService);
        return ApiResult<PaginatedResponse<EventListItemDto>>.Build(
            new PaginatedResponse<EventListItemDto>(dtos, totalCount));
    }

    private static async Task<ApiResult<PaginatedResponse<EventListItemDto>>> GetMyParticipatedEvents(
        [AsParameters] PaginatedRequest request,
        ClaimsPrincipal user,
        IEventRepository eventRepository,
        IS3StorageService storageService,
        CancellationToken ct)
    {
        var userId = user.GetId();
        var (events, totalCount) = await eventRepository.GetParticipatedEventsPaginatedAsync(
            userId, offset: request.Offset, limit: request.Limit, ct: ct);

        var dtos = MapToEventListItemDtos(events, userId, storageService);
        return ApiResult<PaginatedResponse<EventListItemDto>>.Build(
            new PaginatedResponse<EventListItemDto>(dtos, totalCount));
    }

    private static EventListItemDto[] MapToEventListItemDtos(
        Event[] events, Guid userId, IS3StorageService storageService)
    {
        return events.Select(e =>
        {
            var participationStatus = e.EventParticipants
                .Where(p => p.UserId == userId)
                .Select(p => (ParticipantStatus?)p.Status)
                .FirstOrDefault();

            return new EventListItemDto(
                Id: e.Id.ToString(),
                VenueId: e.VenueId.ToString(),
                CoverImageUrl: e.CoverImageS3Key != null
                    ? storageService.S3KeyToPublicFileUrl(e.CoverImageS3Key, e.UpdatedAt)
                    : null,
                Title: e.Title,
                Type: e.Type,
                StartDateTime: e.StartDateTime,
                ParticipantsCount: e.EventParticipants.Count(p => p.Status == ParticipantStatus.Approved),
                ParticipationStatus: participationStatus?.ToString(),
                CreatorId: e.CreatorId.ToString(),
                CreatorAvatarUrl: e.CreatorAvatarS3Key != null
                    ? storageService.S3KeyToPublicFileUrl(e.CreatorAvatarS3Key, null)
                    : null);
        }).ToArray();
    }
}