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
using SharedCore.Policies;
using VenueModule.API.B2B.Companies.Dto;
using VenueModule.API.B2B.Deals;
using VenueModule.API.B2B.Deals.Dto;
using VenueModule.API.B2B.Shared;
using VenueModule.API.B2B.Tags.Dto;
using VenueModule.API.B2B.Venues.Dto;
using VenueModule.Domain.Aggregates;
using VenueModule.Domain.Exceptions;
using VenueModule.Domain.Repositories;
using VenueModule.Domain.Services;

namespace VenueModule.API.B2B.Venues;

public class B2BVenueEndpoints : IEndpoints
{
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var managementApiGroup = app.MapGroup("")
            .WithTags("Venue Management API")
            .RequireAuthorization(AuthPolicies.B2BAdmin);
        managementApiGroup.MapPost("/api/v1/b2b/company/{companyId:guid}/venue", CreateVenue);
        managementApiGroup.MapGet("/api/v1/b2b/venues", GetVenues);
        managementApiGroup.MapGet("/api/v1/b2b/venue/{id:guid}", GetVenue);
        managementApiGroup.MapPatch("/api/v1/b2b/venue/{id:guid}", PatchVenue);
        managementApiGroup.MapDelete("/api/v1/b2b/venue/{id:guid}", DeleteVenue);
        managementApiGroup.MapPost("/api/v1/b2b/venue/{id:guid}/cover/upload-url", GetCoverUploadUrl);
    }

    private static async Task<ApiResult<CreateVenueResponse>> CreateVenue(
        [FromRoute] Guid companyId,
        [FromBody] CreateVenueRequest request,
        ClaimsPrincipal user,
        ICompanyRepository companyRepository,
        IVenueRepository venueRepository,
        ITagRepository tagRepository,
        IS3StorageService storageService,
        CancellationToken ct)
    {
        var company = await companyRepository.GetByIdAsync(
            companyId: companyId,
            ownerId: user.GetId(),
            asNoTracking: true,
            ct: ct);
        if (company == null)
        {
            throw VenueExceptions.CompanyNotFound();
        }

        var venuesCount = await venueRepository.CountByCompanyIdAsync(companyId, ct);
        if (venuesCount >= Validation.MaxVenuesPerCompany)
        {
            throw VenueExceptions.TooManyVenues(company.Name);
        }

        var venueId = Guid.CreateVersion7();
        var venue = new Venue
        {
            Id = venueId,
            QrCode = venueId,
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
            IsVerified = false // venue can be verified only by the support team
        };

        if (request.TagIds.Length > 0)
        {
            var allTags = await tagRepository.GetAllAsync(ct);
            venue.UpdateTags(tagIds: request.TagIds, allTags: allTags);
        }

        await venueRepository.AddAsync(venue, ct);
        await venueRepository.SaveChangesAsync(ct);

        return ApiResult<CreateVenueResponse>.Build(
            new CreateVenueResponse(venue.Id)
        );
    }

    private static async Task<ApiResult<GetVenuesResponse>> GetVenues(
        ClaimsPrincipal user,
        IVenueRepository venueRepository,
        IS3StorageService storageService,
        ILoyaltyService loyaltyService,
        CancellationToken ct)
    {
        var venues = await venueRepository.GetByUserIdAsync(user.GetId(), ct);
        var dtos = venues
            .Select(v => new VenueListItemDto(
                Id: v.Id,
                IsVerified: v.IsVerified,
                CoverImageUrl: v.CoverImageS3Key != null
                    ? storageService.S3KeyToPublicFileUrl(v.CoverImageS3Key, v.UpdatedAt)
                    : null,
                Address: v.Address,
                PinCode: string.IsNullOrEmpty(v.PinCode) ? loyaltyService.GenerateDailyTotpPinCode(v) : v.PinCode,
                IsPinCheckInAllowed: v.IsPinCheckInAllowed
            ))
            .ToArray();
        return ApiResult<GetVenuesResponse>.Build(
            new GetVenuesResponse(dtos)
        );
    }

    private static async Task<ApiResult<GetVenueResponse>> GetVenue(
        [FromRoute] Guid id,
        ClaimsPrincipal user,
        IVenueRepository venueRepository,
        IS3StorageService storageService,
        ILoyaltyService loyaltyService,
        CancellationToken ct)
    {
        var venue = await venueRepository.GetByIdWithAllRelationsAsync(id, ct);
        if (venue == null)
        {
            throw VenueExceptions.VenueNotFound(id);
        }

        return ApiResult<GetVenueResponse>.Build(
            new GetVenueResponse(DomainVenueToDto(storageService, loyaltyService, venue))
        );
    }

    private static async Task<ApiResult.EmptyResult> PatchVenue(
        [FromRoute] Guid id,
        [FromBody] PatchVenueRequest request,
        ClaimsPrincipal user,
        IVenueRepository venueRepository,
        ICompanyRepository companyRepository,
        ITagRepository tagRepository,
        IS3StorageService storageService,
        CancellationToken ct)
    {
        var venue = await VenueOwnerGuard.RequireVenueOwnerAsync(
            venueId: id,
            user: user,
            venueRepository: venueRepository,
            companyRepository: companyRepository,
            asNoTracking: false,
            includes: [v => v.VenueTagLinks],
            ct: ct);

        if (request.TagIds != null)
        {
            var allTags = await tagRepository.GetAllAsync(ct);

            venue.UpdateTags(tagIds: request.TagIds, allTags: allTags);
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
        venue.WebsiteUrl = request.WebsiteUrl ?? venue.WebsiteUrl;
        venue.GoogleMapsUrl = request.GoogleMapsUrl ?? venue.GoogleMapsUrl;
        venue.InstagramUrl = request.InstagramUrl ?? venue.InstagramUrl;
        venue.PhoneNumber = request.PhoneNumber ?? venue.PhoneNumber;
        venue.WorkingHours = request.WorkingHours ?? venue.WorkingHours;
        venue.PinCode = request.PinCode ?? venue.PinCode;
        venue.IsPinCheckInAllowed = request.IsPinCheckInAllowed ?? venue.IsPinCheckInAllowed;
        venue.QrCode = request.QrCode ?? venue.QrCode;
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
            venueId: id,
            user: user,
            venueRepository: venueRepository,
            companyRepository: companyRepository,
            asNoTracking: false,
            ct: ct);
        venue.IsDeleted = true;
        await venueRepository.SaveChangesAsync(ct);
        return ApiResult.Empty;
    }

    private static async Task<ApiResult<GetPresignedUrlResponse>> GetCoverUploadUrl(
        [FromRoute] Guid id,
        [FromBody] GetPresignedUrlRequest request,
        IVenueRepository venueRepository,
        ICompanyRepository companyRepository,
        ClaimsPrincipal user,
        IVenueImageService imageService,
        CancellationToken ct)
    {
        Validation.ThrowIfFileSizeExceed(request.ContentLength, Validation.MaxCoverSizeBytes);
        var venue = await VenueOwnerGuard.RequireVenueOwnerAsync(
            venueId: id,
            user: user,
            venueRepository: venueRepository,
            companyRepository: companyRepository,
            asNoTracking: true,
            ct: ct);

        var urls = await imageService.GetVenueCoverUploadUrlAsync(
            companyId: venue.CompanyId,
            venueId: id,
            contentType: request.ContentType,
            contentLength: request.ContentLength,
            ct: ct);
        return ApiResult<GetPresignedUrlResponse>.Build(
            new GetPresignedUrlResponse(UploadUrl: urls.UploadUrl, PublicDownloadUrl: urls.PublicDownloadUrl)
        );
    }

    private static VenueDto DomainVenueToDto(
        IS3StorageService storageService,
        ILoyaltyService loyaltyService,
        Venue venue)
    {
        return new VenueDto(
            Id: venue.Id.ToString(),
            IsVerified: venue.IsVerified,
            Latitude: venue.Coordinates.Y,
            Longitude: venue.Coordinates.X,
            Address: venue.Address,
            WebsiteUrl: venue.WebsiteUrl,
            GoogleMapsUrl: venue.GoogleMapsUrl,
            InstagramUrl: venue.InstagramUrl,
            PhoneNumber: venue.PhoneNumber,
            WorkingHours: venue.WorkingHours,
            CoverImageUrl: venue.CoverImageS3Key != null
                ? storageService.S3KeyToPublicFileUrl(venue.CoverImageS3Key, venue.UpdatedAt)
                : null,
            PinCode: string.IsNullOrEmpty(venue.PinCode)
                ? loyaltyService.GenerateDailyTotpPinCode(venue)
                : venue.PinCode,
            IsGeneratedPinCode: string.IsNullOrEmpty(venue.PinCode),
            IsPinCheckInAllowed: venue.IsPinCheckInAllowed,
            QrCode: venue.QrCode,
            Tags: venue.VenueTagLinks
                .Select(vt =>
                    new TagDto(Id: vt.TagId, Value: vt.Tag.Value, ParentId: vt.Tag.ParentId)
                )
                .ToArray(),
            Promos: venue.Promos
                .OrderBy(p => p.CreatedAt)
                .Select(p => new PromoDto(
                    Id: p.Id.ToString(),
                    Type: p.Type,
                    Body: p.Body)
                )
                .ToArray(),
            Discounts: venue.Discounts
                .OrderBy(d => d.LoyaltyLevelId)
                .ThenByDescending(d => d.Percent ?? 0)
                .Select(B2BDealsEndpoints.DomainDiscountToDto)
                .ToArray(),
            LoyaltyLevels: venue.Company.LoyaltyLevels
                .OrderBy(l => l.Id)
                .Select(l => new LoyaltyLevelDto(l.Id, l.VisitsUpperBound, l.IsActive))
                .ToArray()
        );
    }
}