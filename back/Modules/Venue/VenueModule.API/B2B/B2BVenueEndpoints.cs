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
using VenueModule.API.B2B.Dto;
using VenueModule.API.Shared;
using VenueModule.Domain.Aggregates;
using VenueModule.Domain.Exceptions;
using VenueModule.Domain.Repositories;
using VenueModule.Domain.Services;
using VenueModule.Domain.ValueObjects;

namespace VenueModule.API.B2B;

public class B2BVenueEndpoints : IEndpoints
{
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var managementApiGroup = app.MapGroup("")
            .WithTags("Venue Management API")
            .RequireAuthorization();
        managementApiGroup.MapPost("/api/v1/company", CreateCompany);
        managementApiGroup.MapGet("/api/v1/company", GetCompany);
        managementApiGroup.MapPatch("/api/v1/company", PatchCompany);
        managementApiGroup.MapPost("/api/v1/venue", CreateVenue);
        managementApiGroup.MapPatch("/api/v1/venue/{id:guid}", PatchVenue);
        managementApiGroup.MapDelete("/api/v1/venue/{id:guid}", DeleteVenue);
        managementApiGroup.MapPost("/api/v1/company/avatar/upload-url", GetAvatarUploadUrl);
        managementApiGroup.MapPost("/api/v1/venue/{venueId:guid}/cover/upload-url", GetCoverUploadUrl);
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
            Id = userId,
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
        [FromBody] PatchCompanyRequest request,
        ClaimsPrincipal user,
        ICompanyRepository repository,
        IS3StorageService storageService,
        CancellationToken ct)
    {
        var company = await repository.GetByOwnerIdAsync(
            ownerId: user.GetId(),
            asNoTracking: true,
            includeVenues: true,
            ct: ct);

        if (company == null)
        {
            throw VenueExceptions.CompanyNotFound();
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
        [FromBody] CreateVenueRequest request,
        ClaimsPrincipal user,
        ICompanyRepository companyRepository,
        IVenueRepository venueRepository,
        IS3StorageService storageService,
        CancellationToken ct)
    {
        var company = await companyRepository.GetByOwnerIdAsync(
            ownerId: user.GetId(),
            asNoTracking: true,
            includeVenues: true,
            ct: ct);
        if (company == null)
        {
            throw VenueExceptions.CompanyNotFound();
        }

        var venue = new Venue
        {
            Id = Guid.CreateVersion7(),
            CompanyId = company.Id,
            CoverImageS3Key = request.CoverImageUrl != null
                ? storageService.PublicFileUrlToS3Key(request.CoverImageUrl)
                : null,
            Coordinates = new Point(request.Longitude, request.Latitude) { SRID = 4326 },
            Address = request.Address,
            WebsiteUrl = request.WebsiteUrl,
            GoogleMapsUrl = request.GoogleMapsUrl,
            InstagramUrl = request.InstagramUrl,
            PhoneNumber = request.PhoneNumber,
            WorkingHours = request.WorkingHours,
            Promos = request.Promos?.Select(p => new Promo { Body = p.Body }).ToList() ?? []
        };

        await venueRepository.AddAsync(venue, ct);
        await venueRepository.SaveChangesAsync(ct);

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
        var venue = await VenueOwnerGuard.RequireVenueOwnerAsync(
            id, user, venueRepository, companyRepository, ct);

        if (request.CoverImageUrl != null)
        {
            venue.CoverImageS3Key = storageService.PublicFileUrlToS3Key(request.CoverImageUrl);
        }

        if (request.Latitude.HasValue && request.Longitude.HasValue)
        {
            venue.Coordinates = new Point(request.Longitude.Value, request.Latitude.Value) { SRID = 4326 };
        }

        venue.Address = request.Address ?? venue.Address;
        venue.WebsiteUrl = request.WebsiteUrl ?? venue.WebsiteUrl;
        venue.GoogleMapsUrl = request.GoogleMapsUrl ?? venue.GoogleMapsUrl;
        venue.InstagramUrl = request.InstagramUrl ?? venue.InstagramUrl;
        venue.PhoneNumber = request.PhoneNumber ?? venue.PhoneNumber;
        venue.WorkingHours = request.WorkingHours ?? venue.WorkingHours;

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

    private static async Task<ApiResult.EmptyResult> DeleteVenue(
        [FromRoute] Guid id,
        ClaimsPrincipal user,
        ICompanyRepository companyRepository,
        IVenueRepository venueRepository,
        CancellationToken ct)
    {
        var venue = await VenueOwnerGuard.RequireVenueOwnerAsync(
            id, user, venueRepository, companyRepository, ct);

        venueRepository.Delete(venue);
        await venueRepository.SaveChangesAsync(ct);
        return ApiResult.Empty;
    }

    private static async Task<ApiResult<GetCompanyResponse>> GetCompany(
        ICompanyRepository repository,
        ClaimsPrincipal user,
        IS3StorageService storageService,
        CancellationToken ct)
    {
        var company = await repository.GetByOwnerIdAsync(
            ownerId: user.GetId(),
            asNoTracking: true,
            includeVenues: true,
            ct: ct);

        if (company == null)
        {
            throw VenueExceptions.CompanyNotFound();
        }

        var companyDto = DomainCompanyToDto(storageService, company);
        var venuesDto = company.Venues
            .Select(v => DomainVenueToDto(storageService, v))
            .ToArray();

        return ApiResult<GetCompanyResponse>.Build(
            new GetCompanyResponse(
                Company: companyDto,
                Venues: venuesDto)
        );
    }

    private static async Task<ApiResult<GetPresignedUrlResponse>> GetAvatarUploadUrl(
        [FromBody] GetPresignedUrlRequest request,
        ClaimsPrincipal user,
        IVenueImageService imageService,
        CancellationToken ct)
    {
        Validation.ThrowIfFileSizeExceed(request.ContentLength, Validation.MaxAvatarSizeBytes);
        var urls = await imageService.GetCompanyAvatarUploadUrlAsync(
            companyId: user.GetId(),
            contentLength: request.ContentLength,
            ct: ct);
        return ApiResult<GetPresignedUrlResponse>.Build(
            new GetPresignedUrlResponse(UploadUrl: urls.UploadUrl, PublicDownloadUrl: urls.PublicDownloadUrl)
        );
    }

    private static async Task<ApiResult<GetPresignedUrlResponse>> GetCoverUploadUrl(
        [FromRoute] Guid venueId,
        [FromBody] GetPresignedUrlRequest request,
        ClaimsPrincipal user,
        IVenueImageService imageService,
        CancellationToken ct)
    {
        Validation.ThrowIfFileSizeExceed(request.ContentLength, Validation.MaxCoverSizeBytes);
        var urls = await imageService.GetVenueCoverUploadUrlAsync(
            companyId: user.GetId(),
            venueId: venueId,
            contentLength: request.ContentLength,
            ct: ct);
        return ApiResult<GetPresignedUrlResponse>.Build(
            new GetPresignedUrlResponse(UploadUrl: urls.UploadUrl, PublicDownloadUrl: urls.PublicDownloadUrl)
        );
    }

    private static VenueDto DomainVenueToDto(IS3StorageService storageService, Venue venue)
    {
        return new VenueDto(
            Id: venue.Id.ToString(),
            Latitude: venue.Coordinates.Y,
            Longitude: venue.Coordinates.X,
            Address: venue.Address,
            WebsiteUrl: venue.WebsiteUrl,
            GoogleMapsUrl: venue.GoogleMapsUrl,
            InstagramUrl: venue.InstagramUrl,
            PhoneNumber: venue.PhoneNumber,
            WorkingHours: venue.WorkingHours,
            CoverImageUrl: venue.CoverImageS3Key != null
                ? storageService.S3KeyToPublicFileUrl(venue.CoverImageS3Key, venue.ChangedAt)
                : null,
            Promos: venue.Promos
                .Select(p => new PromoDto(Body: p.Body))
                .ToArray()
        );
    }

    private static CompanyDto DomainCompanyToDto(IS3StorageService storageService, Company company)
    {
        return new CompanyDto(
            Id: company.Id.ToString(),
            Name: company.Name,
            Email: company.Email,
            AvatarImageUrl: storageService.S3KeyToPublicFileUrl(company.AvatarImageS3Key, company.ChangedAt)
        );
    }
}