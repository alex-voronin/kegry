using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Shared.Infrastructure.Storage;
using SharedCore.Api;
using SharedCore.Api.Dto;
using SharedCore.Extensions;
using SharedCore.Policies;
using VenueModule.API.B2B.Companies.Dto;
using VenueModule.API.B2B.Deals.Dto;
using VenueModule.API.B2B.Shared;
using VenueModule.Domain.Aggregates;
using VenueModule.Domain.Exceptions;
using VenueModule.Domain.Repositories;
using VenueModule.Domain.Services;
using CompanyDto = VenueModule.API.B2B.Companies.Dto.CompanyDto;

namespace VenueModule.API.B2B.Companies;

public class B2BCompanyEndpoints : IEndpoints
{
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var managementApiGroup = app.MapGroup("")
            .WithTags("Company Management API")
            .RequireAuthorization(AuthPolicies.B2BAdmin);
        managementApiGroup.MapPost("/api/v1/companies", CreateCompany);
        managementApiGroup.MapGet("/api/v1/companies", GetCompanies);
        managementApiGroup.MapGet("/api/v1/company/{id:guid}", GetCompany);
        managementApiGroup.MapDelete("/api/v1/company/{id:guid}", DeleteCompany);
        managementApiGroup.MapPatch("/api/v1/company/{id:guid}", PatchCompany);
        managementApiGroup.MapPost("/api/v1/company/{id:guid}/avatar/upload-url", GetAvatarUploadUrl);
    }

    private static async Task<ApiResult<CreateCompanyResponse>> CreateCompany(
        [FromBody] CreateCompanyRequest request,
        ClaimsPrincipal user,
        ICompanyRepository repository,
        IS3StorageService storageService,
        CancellationToken ct)
    {
        var userId = user.GetId();

        var company = new Company
        {
            Id = Guid.CreateVersion7(),
            OwnerId = userId,
            Name = request.Name,
            Email = request.Email,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        company.SetDefaultLoyaltyLevels();

        await repository.AddAsync(company, ct);
        await repository.SaveChangesAsync(ct);

        return ApiResult<CreateCompanyResponse>.Build(new CreateCompanyResponse(company.Id));
    }

    private static async Task<ApiResult<GetCompaniesResponse>> GetCompanies(
        ICompanyRepository repository,
        ClaimsPrincipal user,
        IS3StorageService storageService,
        CancellationToken ct)
    {
        var companies = await repository.GetByOwnerIdAsync(
            ownerId: user.GetId(),
            asNoTracking: true,
            includeVenues: false,
            ct: ct);

        var dtos = companies
            .Select(c => DomainCompanyToDto(storageService, c))
            .ToArray();
        return ApiResult<GetCompaniesResponse>.Build(
            new GetCompaniesResponse(Companies: dtos)
        );
    }

    private static async Task<ApiResult<GetCompanyResponse>> GetCompany(
        [FromRoute] Guid id,
        ICompanyRepository repository,
        ClaimsPrincipal user,
        IS3StorageService storageService,
        CancellationToken ct)
    {
        var company = await repository.GetByIdAsync(
            companyId: id,
            ownerId: user.GetId(),
            asNoTracking: true,
            includeVenues: true,
            includeLoyaltyLevels: true,
            ignoreVerifiedFilter: true,
            ct: ct);
        if (company == null)
        {
            throw VenueExceptions.CompanyNotFound();
        }

        var companyDto = DomainCompanyToDto(storageService, company);
        var venuesDto = company.Venues
            .Select(v => DomainVenueToListItemDto(storageService, v))
            .ToArray();

        return ApiResult<GetCompanyResponse>.Build(
            new GetCompanyResponse(
                Company: companyDto,
                Venues: venuesDto)
        );
    }

    private static async Task<ApiResult.EmptyResult> PatchCompany(
        [FromRoute] Guid id,
        [FromBody] PatchCompanyRequest request,
        ClaimsPrincipal user,
        ICompanyRepository repository,
        IS3StorageService storageService,
        CancellationToken ct)
    {
        var company = await repository.GetByIdAsync(
            companyId: id,
            ownerId: user.GetId(),
            asNoTracking: true,
            ignoreVerifiedFilter: true,
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

    private static async Task<IResult> DeleteCompany(
        [FromRoute] Guid id,
        ClaimsPrincipal user,
        ICompanyRepository repository,
        CancellationToken ct)
    {
        var company = await repository.GetByIdAsync(
            companyId: id,
            ownerId: user.GetId(),
            includeVenues: true,
            asNoTracking: true,
            ignoreVerifiedFilter: true,
            ct: ct);
        if (company == null)
        {
            throw VenueExceptions.CompanyNotFound();
        }

        foreach (var venue in company.Venues)
        {
            venue.IsDeleted = true;
        }
        company.IsDeleted = true;
        await repository.SaveChangesAsync(ct);

        return Results.Ok(ApiResult.Empty);
    }

    private static async Task<ApiResult<GetPresignedUrlResponse>> GetAvatarUploadUrl(
        [FromRoute] Guid id,
        [FromBody] GetPresignedUrlRequest request,
        ICompanyRepository repository,
        ClaimsPrincipal user,
        IVenueImageService imageService,
        CancellationToken ct)
    {
        Validation.ThrowIfFileSizeExceed(request.ContentLength, Validation.MaxAvatarSizeBytes);
        var company = await repository.GetByIdAsync(
            companyId: id,
            ownerId: user.GetId(),
            asNoTracking: true,
            ignoreVerifiedFilter: true,
            ct: ct);
        if (company == null)
        {
            throw VenueExceptions.CompanyNotFound();
        }

        var urls = await imageService.GetCompanyAvatarUploadUrlAsync(
            companyId: id,
            contentLength: request.ContentLength,
            ct: ct);
        return ApiResult<GetPresignedUrlResponse>.Build(
            new GetPresignedUrlResponse(UploadUrl: urls.UploadUrl, PublicDownloadUrl: urls.PublicDownloadUrl)
        );
    }

    private static VenueListItemDto DomainVenueToListItemDto(IS3StorageService storageService, Venue venue)
    {
        return new VenueListItemDto(
            Id: venue.Id.ToString(),
            IsVerified: venue.IsVerified,
            Address: venue.Address,
            CoverImageUrl: venue.CoverImageS3Key != null
                ? storageService.S3KeyToPublicFileUrl(venue.CoverImageS3Key, venue.ChangedAt)
                : null
        );
    }

    private static CompanyDto DomainCompanyToDto(IS3StorageService storageService, Company company)
    {
        return new CompanyDto(
            Id: company.Id.ToString(),
            Name: company.Name,
            Email: company.Email,
            AvatarImageUrl: company.AvatarImageS3Key != null
                ? storageService.S3KeyToPublicFileUrl(company.AvatarImageS3Key, company.ChangedAt)
                : null,
            LoyaltyLevels: company.LoyaltyLevels
                .Select(l => new LoyaltyLevelDto(l.Id, l.VisitsUpperBound, l.IsActive))
                .ToArray()
        );
    }
}