using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using NetTopologySuite.Geometries;
using Shared.Infrastructure.Storage;
using SharedCore.Api;
using SharedCore.Api.Dto;
using SharedCore.Extensions;
using SharedCore.Geolocation;
using VenueModule.API.Dto;
using VenueModule.Domain.Aggregates;
using VenueModule.Domain.Exceptions;
using VenueModule.Domain.Repositories;
using VenueModule.Domain.Services;
using VenueModule.Domain.ValueObjects;

namespace VenueModule.API;

public class VenueEndpoints : IEndpoints
{
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var managementApiGroup = app.MapGroup("")
            .WithTags("Venue Management API")
            .RequireAuthorization();
        managementApiGroup.MapPost("/api/v1/company", CreateCompany);
        managementApiGroup.MapPatch("/api/v1/company/{companyId:guid?}", PatchCompany);
        managementApiGroup.MapPost("/api/v1/company/{companyId:guid?}/venue", CreateVenue);
        managementApiGroup.MapPatch("/api/v1/venue/{id:guid?}", PatchVenue);
        managementApiGroup.MapGet("/api/v1/company/{companyId:guid?}", GetCompany);
        managementApiGroup.MapPost("/api/v1/venue/{id:guid?}/check-in", CheckInToVenue);
        managementApiGroup.MapPost("/api/v1/company/{companyId:guid?}/avatar/upload-url", GetAvatarUploadUrl);
        managementApiGroup.MapPost("/api/v1/company/{companyId:guid?}/venue/{venueId:guid?}/cover/upload-url",
            GetCoverUploadUrl);

        var usersApiGroup = app.MapGroup("")
            .WithTags("Venue Users API")
            .RequireAuthorization();
        usersApiGroup.MapPost("/api/v1/venues", GetVenues);
    }

    private static async Task<ApiResult.EmptyResult> CreateCompany(
        [FromBody] CreateCompanyRequest request,
        ClaimsPrincipal user,
        ICompanyRepository repository,
        IS3StorageService storageService,
        CancellationToken ct)
    {
        var userId = user.GetId();

        var company = new Company
        {
            Id = Guid.NewGuid(),
            OwnerId = userId,
            Name = request.Name,
            Email = request.Email,
            AvatarImageS3Key = storageService.PublicFileUrlToS3Key(request.AvatarImageUrl),
            Venues = []
        };

        await repository.AddAsync(company, ct);
        await repository.SaveChangesAsync(ct);

        return ApiResult.Empty;
    }

    private static async Task<ApiResult.EmptyResult> PatchCompany(
        [FromRoute] Guid companyId,
        [FromBody] PatchCompanyRequest request,
        ClaimsPrincipal user,
        ICompanyRepository repository,
        IS3StorageService storageService,
        CancellationToken ct)
    {
        var company = await repository.GetByIdAsync(companyId, ct: ct);

        if (company == null)
        {
            throw VenueExceptions.VenueNotFound(companyId);
        }

        if (company.OwnerId != user.GetId())
        {
            throw VenueExceptions.SomeoneElseCompany(companyId);
        }

        company.Name = request.Name ?? company.Name;
        company.Email = request.Email ?? company.Email;

        if (request.AvatarImageUrl != null)
        {
            company.AvatarImageS3Key = storageService.PublicFileUrlToS3Key(request.AvatarImageUrl);
        }

        await repository.SaveChangesAsync(ct);

        return ApiResult.Empty;
    }

    private static async Task<ApiResult.EmptyResult> CreateVenue(
        [FromRoute] Guid companyId,
        [FromBody] CreateVenueRequest request,
        ClaimsPrincipal user,
        ICompanyRepository repository,
        IS3StorageService storageService,
        CancellationToken ct)
    {
        var company = await repository.GetByIdAsync(
            companyId,
            includes: [c => c.Venues],
            ct: ct);

        if (company == null)
        {
            throw VenueExceptions.VenueNotFound(companyId);
        }

        if (company.OwnerId != user.GetId())
        {
            throw VenueExceptions.SomeoneElseCompany(companyId);
        }

        var venue = new Venue
        {
            Id = Guid.NewGuid(),
            CompanyId = company.Id,
            CoverImageS3Key = request.CoverImageUrl != null
                ? storageService.PublicFileUrlToS3Key(request.CoverImageUrl)
                : null,
            Coordinates = new Point(request.Longitude, request.Latitude) { SRID = 4326 },
            Address = request.Address,
            GoogleMapsUrl = request.GoogleMapsUrl,
            InstagramUrl = request.InstagramUrl,
            PhoneNumber = request.PhoneNumber,
            Promos = request.Promos?.Select(p => new Promo { Body = p.Body }).ToList() ?? []
        };

        company.Venues.Add(venue);
        await repository.SaveChangesAsync(ct);

        return ApiResult.Empty;
    }

    private static async Task<ApiResult.EmptyResult> PatchVenue(
        [FromRoute] Guid id,
        [FromBody] PatchVenueRequest request,
        ClaimsPrincipal user,
        IVenueRepository venueRepository,
        ICompanyRepository companyRepository,
        IS3StorageService storageService,
        CancellationToken ct)
    {
        var venue = await venueRepository.GetByIdAsync(
            id,
            includes: [v => v.Promos],
            ct: ct);

        if (venue == null)
        {
            throw VenueExceptions.VenueNotFound(id);
        }

        var company = await companyRepository.GetByIdAsync(venue.CompanyId, ct: ct);
        if (company == null)
        {
            throw VenueExceptions.VenueNotFound(venue.CompanyId);
        }

        if (company.OwnerId != user.GetId())
        {
            throw VenueExceptions.SomeoneElseCompany(company.Id);
        }

        if (request.CoverImageUrl != null)
        {
            venue.CoverImageS3Key = storageService.PublicFileUrlToS3Key(request.CoverImageUrl);
        }

        if (request.Latitude.HasValue && request.Longitude.HasValue)
        {
            venue.Coordinates = new Point(request.Longitude.Value, request.Latitude.Value) { SRID = 4326 };
        }

        venue.Address = request.Address ?? venue.Address;
        venue.GoogleMapsUrl = request.GoogleMapsUrl ?? venue.GoogleMapsUrl;
        venue.InstagramUrl = request.InstagramUrl ?? venue.InstagramUrl;
        venue.PhoneNumber = request.PhoneNumber ?? venue.PhoneNumber;

        if (request.Promos != null)
        {
            venue.Promos = request.Promos.Select(p => new Promo
            {
                Body = p.Body
            }).ToList();
        }

        await venueRepository.SaveChangesAsync(ct);

        return ApiResult.Empty;
    }

    private static async Task<ApiResult<GetCompanyResponse>> GetCompany(
        [FromRoute] Guid companyId,
        ICompanyRepository repository,
        IS3StorageService storageService,
        CancellationToken ct)
    {
        var company = await repository.GetByIdAsync(
            companyId,
            asNoTracking: true,
            includes: [c => c.Venues],
            ct: ct);

        if (company == null)
        {
            throw VenueExceptions.VenueNotFound(companyId);
        }

        var dto = new CompanyDto(
            Id: company.Id.ToString(),
            Name: company.Name,
            Email: company.Email,
            AvatarImageUrl: storageService.S3KeyToPublicFileUrl(company.AvatarImageS3Key, company.ChangedAt),
            Venues: company.Venues
                .Select(v => new VenueDto(
                    Id: v.Id.ToString(),
                    Latitude: v.Coordinates.Y,
                    Longitude: v.Coordinates.X,
                    Address: v.Address,
                    GoogleMapsUrl: v.GoogleMapsUrl,
                    InstagramUrl: v.InstagramUrl,
                    PhoneNumber: v.PhoneNumber,
                    CoverImageUrl: v.CoverImageS3Key != null
                        ? storageService.S3KeyToPublicFileUrl(v.CoverImageS3Key, company.ChangedAt)
                        : null,
                    CompanyAvatarImageUrl: storageService.S3KeyToPublicFileUrl(company.AvatarImageS3Key,
                        company.ChangedAt),
                    Promos: v.Promos
                        .Select(p => new PromoDto(Body: p.Body))
                        .ToArray()))
                .ToArray()
        );

        return ApiResult<GetCompanyResponse>.Build(new GetCompanyResponse(dto));
    }

    private static async Task<ApiResult<EmptyResponse>> CheckInToVenue(
        [FromRoute] Guid id,
        [FromBody] CheckInRequest request,
        ClaimsPrincipal user,
        IVenueRepository venueRepository,
        IVisitRepository visitRepository,
        CancellationToken ct)
    {
        var userId = user.GetId();

        var venue = await venueRepository.GetByIdAsync(
            id,
            asNoTracking: true,
            ct: ct);

        if (venue == null)
        {
            throw VenueExceptions.VenueNotFound(id);
        }

        var distanceMeters = Haversine.DistanceMeters(
            request.Latitude, request.Longitude,
            venue.Coordinates.Y, venue.Coordinates.X);

        if (distanceMeters > Validation.MaxCheckInDistanceMeters)
        {
            throw VenueExceptions.TooFarFromVenue();
        }

        var visit = new Visit
        {
            UserId = userId,
            VenueId = id,
            CheckedInAt = DateTimeOffset.UtcNow
        };

        await visitRepository.AddAsync(visit, ct);
        await visitRepository.SaveChangesAsync(ct);

        return ApiResult<EmptyResponse>.Build(EmptyResponse.Empty);
    }

    private static async Task<ApiResult<GetPresignedUrlResponse>> GetAvatarUploadUrl(
        [FromBody] GetPresignedUrlRequest request,
        [FromQuery] Guid companyId,
        ClaimsPrincipal user,
        IVenueImageService imageService,
        CancellationToken ct)
    {
        Validation.ThrowIfFileSizeExceed(request.ContentLength, Validation.MaxAvatarSizeBytes);
        var urls = await imageService.GetCompanyAvatarUploadUrlAsync(
            companyId: companyId,
            contentLength: request.ContentLength,
            ct: ct);
        return ApiResult<GetPresignedUrlResponse>.Build(
            new GetPresignedUrlResponse(UploadUrl: urls.UploadUrl, PublicDownloadUrl: urls.PublicDownloadUrl)
        );
    }

    private static async Task<ApiResult<GetPresignedUrlResponse>> GetCoverUploadUrl(
        [FromQuery] Guid companyId,
        [FromQuery] Guid venueId,
        [FromBody] GetPresignedUrlRequest request,
        IVenueImageService imageService,
        CancellationToken ct)
    {
        Validation.ThrowIfFileSizeExceed(request.ContentLength, Validation.MaxCoverSizeBytes);
        var urls = await imageService.GetVenueCoverUploadUrlAsync(
            companyId: companyId,
            venueId: venueId,
            contentLength: request.ContentLength,
            ct: ct);
        return ApiResult<GetPresignedUrlResponse>.Build(
            new GetPresignedUrlResponse(UploadUrl: urls.UploadUrl, PublicDownloadUrl: urls.PublicDownloadUrl)
        );
    }

    private static async Task<ApiResult<PaginatedResponse<VenueDto>>> GetVenues(
        [AsParameters] PaginatedRequest request,
        ICompanyRepository repository,
        IS3StorageService storageService,
        CancellationToken ct)
    {
        var (venues, totalCount) = await repository.GetVenuesPaginatedAsync(request.Offset, request.Limit, ct);

        var dtos = new VenueDto[venues.Length];
        for (var i = 0; i < venues.Length; i++)
        {
            var v = venues[i];
            dtos[i] = new VenueDto(
                Id: v.Id.ToString(),
                Latitude: v.Coordinates.Y,
                Longitude: v.Coordinates.X,
                Address: v.Address,
                GoogleMapsUrl: v.GoogleMapsUrl,
                InstagramUrl: v.InstagramUrl,
                PhoneNumber: v.PhoneNumber,
                CoverImageUrl: v.CoverImageS3Key != null
                    ? storageService.S3KeyToPublicFileUrl(v.CoverImageS3Key, v.ChangedAt)
                    : null,
                CompanyAvatarImageUrl: storageService.S3KeyToPublicFileUrl(v.Company.AvatarImageS3Key,
                    v.Company.ChangedAt),
                Promos: v.Promos
                    .Select(p => new PromoDto(Body: p.Body))
                    .ToArray()
            );
        }

        return ApiResult<PaginatedResponse<VenueDto>>.Build(
            new PaginatedResponse<VenueDto>(dtos, totalCount)
        );
    }
}