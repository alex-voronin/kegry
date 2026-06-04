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

        usersApiGroup.MapGet("/api/v1/events/map", GetEventsMap);
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
    }

    private static async Task<ApiResult<VenueEventsDto[]>> GetEventsMap(
        [AsParameters] GetEventsMapRequest request,
        ClaimsPrincipal user,
        IEventRepository eventRepository,
        IS3StorageService storageService,
        CancellationToken ct)
    {
        var userId = user.GetId();
        var result = await eventRepository.GetEventsInBoundsAsync(
            request.MinLat, request.MaxLat,
            request.MinLng, request.MaxLng,
            request.Type, request.DateFrom, request.DateTo, 20, userId, ct);

        EventMapItemDto MapEvent(EventWithIsJoined ewj)
        {
            return new EventMapItemDto(
                Id: ewj.Event.Id.ToString(),
                CoverImageUrl: ewj.Event.CoverImageS3Key != null
                    ? storageService.S3KeyToPublicFileUrl(ewj.Event.CoverImageS3Key, ewj.Event.UpdatedAt)
                    : null,
                Title: ewj.Event.Title,
                Type: ewj.Event.Type,
                StartDateTime: ewj.Event.StartDateTime,
                VenueId: ewj.Event.VenueId.ToString(),
                IsJoined: ewj.IsJoined);
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
        EventContext dbContext,
        IS3StorageService storageService,
        CancellationToken ct)
    {
        var ev = await eventRepository.GetByIdWithRelationsAsync(id, ct);
        if (ev == null)
            throw EventExceptions.EventNotFound(id);

        var userId = user.GetId();
        var isCreator = ev.CreatorId == userId;
        var isJoined = ev.EventParticipants.Any(p => p.UserId == userId);

        var venue = await dbContext.Venues.FirstOrDefaultAsync(v => v.Id == ev.VenueId, ct);
        var creatorUser = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == ev.CreatorId, ct);

        var contacts = ev.EventContacts
            .Where(c => c.IsPublic || isCreator || isJoined)
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
            VenueAddress: venue?.Address ?? "",
            VenueLatitude: venue?.Coordinates.Y ?? 0,
            VenueLongitude: venue?.Coordinates.X ?? 0,
            CreatorId: ev.CreatorId.ToString(),
            CreatorFirstName: creatorUser?.FirstName,
            CreatorLastName: creatorUser?.LastName,
            CreatorAvatarUrl: creatorUser?.AvatarImageS3Key != null
                ? storageService.S3KeyToPublicFileUrl(creatorUser.AvatarImageS3Key, null)
                : null,
            IsVisibleOnMap: ev.IsVisibleOnMap,
            MaxParticipants: ev.MaxParticipants,
            RequiresApproval: ev.RequiresApproval,
            ParticipantsCount: ev.EventParticipants.Count,
            Contacts: contacts,
            HobbyIds: ev.EventHobbyLinks.Select(h => h.HobbyId).ToArray(),
            IsJoined: isJoined,
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
                ParticipantsCount: e.EventParticipants.Count,
                IsJoined: e.EventParticipants.Any(p => p.UserId == userId));
        }).ToArray();

        return ApiResult<PaginatedResponse<EventListItemDto>>.Build(
            new PaginatedResponse<EventListItemDto>(dtos, totalCount));
    }

    private static async Task<ApiResult<EventDto>> CreateEvent(
        [FromBody] CreateEventRequest request,
        ClaimsPrincipal user,
        IEventRepository eventRepository,
        EventContext dbContext,
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

        await eventRepository.AddAsync(ev, ct);
        await eventRepository.SaveChangesAsync(ct);

        return await GetEvent(eventId, user, eventRepository, dbContext, storageService, ct);
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

        return await GetEvent(id, user, eventRepository, dbContext, storageService, ct);
    }

    private static async Task<ApiResult.EmptyResult> DeleteEvent(
        [FromRoute] Guid id,
        ClaimsPrincipal user,
        IEventRepository eventRepository,
        CancellationToken ct)
    {
        var ev = await eventRepository.GetByIdAsync(id, ct: ct);
        if (ev == null)
            throw EventExceptions.EventNotFound(id);
        if (ev.CreatorId != user.GetId())
            throw EventExceptions.NotEventCreator();

        eventRepository.Delete(ev);
        await eventRepository.SaveChangesAsync(ct);
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
        await eventActionService.ApproveParticipantAsync(id, user.GetId(), userId);
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
}