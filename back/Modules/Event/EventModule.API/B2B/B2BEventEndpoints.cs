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
using SharedCore.Policies;
using EventModule.API.B2B.Dto;
using EventModule.API.B2C.Dto;
using EventModule.Domain.Aggregates;
using EventModule.Domain.Enums;
using EventModule.Domain.Exceptions;
using EventModule.Domain.Repositories;
using EventModule.Domain.Services;
using EventModule.Infrastructure.Persistence;

namespace EventModule.API.B2B;

public class B2BEventEndpoints : IEndpoints
{
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var managementApiGroup = app.MapGroup("")
            .WithTags("Event Management API")
            .RequireAuthorization(AuthPolicies.B2BAdmin);

        managementApiGroup.MapGet("/api/v1/b2b/events", GetEvents);
        managementApiGroup.MapGet("/api/v1/b2b/event/{id:guid}", GetEvent);
        managementApiGroup.MapPost("/api/v1/b2b/events", CreateEvent);
        managementApiGroup.MapPatch("/api/v1/b2b/event/{id:guid}", UpdateEvent);
        managementApiGroup.MapDelete("/api/v1/b2b/event/{id:guid}", DeleteEvent);
        managementApiGroup.MapPost("/api/v1/b2b/event/{id:guid}/cover/upload-url", GetCoverUploadUrl);
        managementApiGroup.MapGet("/api/v1/b2b/event/{id:guid}/participants", GetParticipants);
    }

    private static async Task<ApiResult<PaginatedResponse<B2BEventListItemDto>>> GetEvents(
        [AsParameters] B2BGetEventsRequest request,
        ClaimsPrincipal user,
        IEventRepository eventRepository,
        EventContext dbContext,
        IS3StorageService storageService,
        CancellationToken ct)
    {
        var userId = user.GetId();
        var (events, totalCount) = await eventRepository.GetOwnEventsPaginatedAsync(
            userId, request.VenueId, request.Type,
            request.DateFrom, request.DateTo,
            request.Offset, request.Limit, ct);

        var venueIds = events.Select(e => e.VenueId).Distinct().ToArray();
        var venues = await dbContext.Venues
            .Where(v => venueIds.Contains(v.Id))
            .ToDictionaryAsync(v => v.Id, ct);

        var dtos = events.Select(e => new B2BEventListItemDto(
            Id: e.Id.ToString(),
            CoverImageUrl: e.CoverImageS3Key != null
                ? storageService.S3KeyToPublicFileUrl(e.CoverImageS3Key, e.UpdatedAt)
                : null,
            Title: e.Title,
            Type: e.Type,
            StartDateTime: e.StartDateTime,
            VenueId: e.VenueId.ToString(),
            VenueAddress: venues.GetValueOrDefault(e.VenueId)?.Address ?? "",
            IsVisibleOnMap: e.IsVisibleOnMap,
            MaxParticipants: e.MaxParticipants,
            RequiresApproval: e.RequiresApproval,
            ParticipantsCount: e.EventParticipants.Count(p => p.Status == ParticipantStatus.Approved))).ToArray();

        return ApiResult<PaginatedResponse<B2BEventListItemDto>>.Build(
            new PaginatedResponse<B2BEventListItemDto>(dtos, totalCount));
    }

    private static async Task<ApiResult<EventDto>> GetEvent(
        [FromRoute] Guid id,
        ClaimsPrincipal user,
        IEventRepository eventRepository,
        EventContext dbContext,
        IS3StorageService storageService,
        CancellationToken ct)
    {
        var userId = user.GetId();
        var ev = await eventRepository.GetByIdWithRelationsAsync(id, ct);
        if (ev == null)
            throw EventExceptions.EventNotFound(id);
        if (ev.CreatorId != userId)
            throw EventExceptions.NotEventCreator();

        var contacts = ev.EventContacts
            .Select(c => new EventContactDto(c.Id, c.Type, c.Value, c.IsPublic))
            .ToArray();

        var participationStatus = ev.EventParticipants
            .FirstOrDefault(p => p.UserId == userId)?.Status;
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
            PendingParticipantIds: [],
            ApprovedParticipantIds: [],
            Contacts: contacts,
            HobbyIds: ev.EventHobbyLinks.Select(h => h.HobbyId).ToArray(),
            ParticipationStatus: participationStatus?.ToString(),
            IsCreator: true);

        return ApiResult<EventDto>.Build(dto);
    }

    private static async Task<ApiResult<EventDto>> CreateEvent(
        [FromBody] B2BCreateEventRequest request,
        ClaimsPrincipal user,
        IEventRepository eventRepository,
        EventContext dbContext,
        IS3StorageService storageService,
        CancellationToken ct)
    {
        var userId = user.GetId();
        await RequireVenueOwnershipAsync(request.VenueId, userId, dbContext, ct);

        var eventId = Guid.CreateVersion7();

        var ev = new Event(
            id: eventId,
            coverImageS3Key: request.CoverImageUrl != null
                ? storageService.PublicFileUrlToS3Key(request.CoverImageUrl)
                : null,
            title: request.Title,
            description: request.Description,
            startDateTime: request.StartDateTime,
            type: EventType.Event,
            venueId: request.VenueId,
            creatorId: userId,
            creatorAvatarS3Key: null,
            maxParticipants: null,
            requiresApproval: false);
        ev.IsVisibleOnMap = true;

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

        return await BuildEventDetail(eventId, userId, eventRepository, storageService, ct);
    }

    private static async Task<ApiResult<EventDto>> UpdateEvent(
        [FromRoute] Guid id,
        [FromBody] B2BUpdateEventRequest request,
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

        if (request.CoverImageUrl != null)
            ev.CoverImageS3Key = storageService.PublicFileUrlToS3Key(request.CoverImageUrl);

        if (request.Title != null) ev.Title = request.Title;
        if (request.Description != null) ev.Description = request.Description;
        if (request.StartDateTime.HasValue) ev.StartDateTime = request.StartDateTime.Value;

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

        return await BuildEventDetail(id, user.GetId(), eventRepository, storageService, ct);
    }

    private static async Task<ApiResult.EmptyResult>
 DeleteEvent(
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

        var urls = await imageService.GetEventCoverUploadUrlAsync(
            eventId: id,
            contentType: request.ContentType,
            contentLength: request.ContentLength,
            ct: ct);

        return ApiResult<GetPresignedUrlResponse>.Build(
            new GetPresignedUrlResponse(UploadUrl: urls.UploadUrl, PublicDownloadUrl: urls.PublicDownloadUrl));
    }

    private static async Task<ApiResult<EventDto>> BuildEventDetail(
        Guid eventId,
        Guid userId,
        IEventRepository eventRepository,
        IS3StorageService storageService,
        CancellationToken ct)
    {
        var ev = await eventRepository.GetByIdWithRelationsAsync(eventId, ct);
        if (ev == null)
            throw EventExceptions.EventNotFound(eventId);

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
                ? storageService.S3KeyToPublicFileUrl(ev.CreatorAvatarS3Key, null)
                : null,
            IsVisibleOnMap: ev.IsVisibleOnMap,
            MaxParticipants: ev.MaxParticipants,
            RequiresApproval: ev.RequiresApproval,
            ParticipantsCount: ev.EventParticipants.Count(p => p.Status == ParticipantStatus.Approved),
            PendingApprovalsCount: ev.EventParticipants.Count(p => p.Status == ParticipantStatus.Pending),
            PendingParticipantIds: ev.EventParticipants.Where(p => p.Status == ParticipantStatus.Pending).Select(p => p.UserId).Take(50).ToArray(),
            ApprovedParticipantIds: ev.EventParticipants.Where(p => p.Status == ParticipantStatus.Approved).Select(p => p.UserId).Take(50).ToArray(),
            Contacts: contacts,
            HobbyIds: ev.EventHobbyLinks.Select(h => h.HobbyId).ToArray(),
            ParticipationStatus: participationStatus?.ToString(),
            IsCreator: isCreator);

        return ApiResult<EventDto>.Build(dto);
    }

    private static async Task RequireVenueOwnershipAsync(
        Guid venueId,
        Guid userId,
        EventContext dbContext,
        CancellationToken ct)
    {
        var venue = await dbContext.Venues.FirstOrDefaultAsync(v => v.Id == venueId, ct);
        if (venue == null)
            throw EventExceptions.VenueNotFound(venueId);

        var company = await dbContext.Companies.FirstOrDefaultAsync(c => c.Id == venue.CompanyId, ct);
        if (company == null || company.OwnerId != userId)
            throw EventExceptions.NotVenueOwner();
    }
}
