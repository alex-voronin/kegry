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
using EventModule.Domain.Aggregates.ReadOnly;
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

        managementApiGroup.MapPost("/api/v1/b2b/events", CreateEvent);
        managementApiGroup.MapPatch("/api/v1/b2b/events/{id:guid}", UpdateEvent);
        managementApiGroup.MapDelete("/api/v1/b2b/events/{id:guid}", DeleteEvent);
        managementApiGroup.MapPost("/api/v1/b2b/events/{id:guid}/cover/upload-url", GetCoverUploadUrl);
        managementApiGroup.MapGet("/api/v1/b2b/events/{id:guid}/participants", GetParticipants);
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
            type: request.Type,
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

        return await BuildEventDetail(eventId, userId, eventRepository, dbContext, storageService, ct);
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
        if (request.MaxParticipants.HasValue) ev.MaxParticipants = request.MaxParticipants;
        if (request.RequiresApproval.HasValue) ev.RequiresApproval = request.RequiresApproval.Value;

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

        return await BuildEventDetail(id, user.GetId(), eventRepository, dbContext, storageService, ct);
    }

    private static async Task<ApiResult.EmptyResult>
 DeleteEvent(
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
        EventContext dbContext,
        IS3StorageService storageService,
        CancellationToken ct)
    {
        var ev = await eventRepository.GetByIdWithRelationsAsync(eventId, ct);
        if (ev == null)
            throw EventExceptions.EventNotFound(eventId);

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
