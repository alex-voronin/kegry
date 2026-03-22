using Shared.Infrastructure.Storage;
using VenueModule.API.B2C.Dto;
using VenueModule.API.Shared;
using GetVenueResponse = VenueModule.API.B2C.Dto.GetVenueResponse;

namespace VenueModule.API.B2C;

using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using SharedCore.Api;
using SharedCore.Api.Dto;
using SharedCore.Extensions;
using Domain.Aggregates;
using Domain.Exceptions;
using Domain.Repositories;
using Domain.Services;

public class B2CVenueEndpoints : IEndpoints
{
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var usersApiGroup = app.MapGroup("")
            .WithTags("Venue User API")
            .RequireAuthorization();
        usersApiGroup.MapPost("/api/v1/venues", GetVenues);
        usersApiGroup.MapGet("/api/v1/venues/{id:guid}", GetVenue);
        usersApiGroup.MapGet("/api/v1/venues/{id:guid}/discounts", GetDiscountsByWeekday);
        usersApiGroup.MapPost("/api/v1/venues/{qrCode:guid}/qr-check-in", CheckInToVenue);
    }

    private static async Task<ApiResult<CheckInResponse>> CheckInToVenue(
        [FromRoute] Guid qrCode,
        [FromBody] CheckInRequest request,
        ClaimsPrincipal user,
        ILoyaltyService loyaltyService,
        CancellationToken ct)
    {
        var userId = user.GetId();
        var venueId = await loyaltyService.CheckInToVenue(
            userId: userId,
            qrCode: qrCode,
            userLatitude: request.Latitude,
            userLongitude: request.Longitude,
            ct: ct);

        return ApiResult<CheckInResponse>.Build(
            new CheckInResponse(venueId)
        );
    }

    private static async Task<ApiResult<PaginatedResponse<VenueListItemDto>>> GetVenues(
        [AsParameters] GetVenuesRequest request,
        IVenueRepository repository,
        IS3StorageService storageService,
        CancellationToken ct)
    {
        var (venues, totalCount) = await repository.GetVenuesPaginatedAsync(
            latitude: request.Latitude,
            longitude: request.Longitude,
            offset: request.Offset,
            limit: request.Limit,
            ct: ct);
        var dtos = venues
            .Select(v =>
                new VenueListItemDto(
                    Id: v.Venue.Id.ToString(),
                    CompanyName: v.Venue.Company.Name,
                    CompanyAvatarImageUrl: v.Venue.Company.AvatarImageS3Key != null
                        ? storageService
                            .S3KeyToPublicFileUrl(v.Venue.Company.AvatarImageS3Key, v.Venue.Company.ChangedAt)
                        : null,
                    Address: v.Venue.Address,
                    DistanceMeters: (int)v.DistanceMeters,
                    GoogleMapsUrl: v.Venue.GoogleMapsUrl
                )
            )
            .ToArray();
        return ApiResult<PaginatedResponse<VenueListItemDto>>.Build(
            new PaginatedResponse<VenueListItemDto>(dtos, totalCount)
        );
    }

    private static async Task<ApiResult<GetVenueResponse>> GetVenue(
        [FromRoute] Guid id,
        ClaimsPrincipal user,
        IVenueRepository repository,
        ILoyaltyService loyaltyService,
        IS3StorageService storageService,
        CancellationToken ct)
    {
        var venue = await repository.GetByIdAsync(id, includes: [v => v.Company], ct: ct);
        if (venue == null)
        {
            throw VenueExceptions.VenueNotFound(id);
        }

        var companyDto = DomainCompanyToDto(storageService, venue.Company);
        var venueDto = DomainVenueToDto(storageService, venue);
        var loyaltyLevel = await loyaltyService.GetLoyaltyLevelWithDiscountAsync(
            userId: user.GetId(),
            venueId: id,
            ct: ct);

        var loyaltyLevelDto = new LoyaltyLevelWithDiscountDto(
            UserLoyaltyLevel: loyaltyLevel.UserLoyaltyLevel,
            VisitsCount: loyaltyLevel.VisitsCount,
            VisitsLowerBound: loyaltyLevel.VisitsLowerBound,
            VisitsUpperBound: loyaltyLevel.VisitsUpperBound,
            Discount: loyaltyLevel.Discount,
            NextLevelDiscount: loyaltyLevel.NextLevelDiscount
        );

        return ApiResult<GetVenueResponse>.Build(
            new GetVenueResponse(
                Company: companyDto,
                Venue: venueDto,
                LoyaltyLevel: loyaltyLevelDto)
        );
    }

    private static async Task<ApiResult<GetDiscountsByWeekdayResponse>> GetDiscountsByWeekday(
        [FromRoute] Guid id,
        ClaimsPrincipal user,
        ILoyaltyService loyaltyService,
        IS3StorageService storageService,
        CancellationToken ct)
    {
        var discountToDto = (CurrentAndNextLevelDiscount? domainDiscount) =>
            new CurrentAndNextLevelDiscountDto(
                CurrentLevelDiscount: domainDiscount?.Discount ?? 0,
                NextLevelDiscount: domainDiscount?.NextLevelDiscount
            );

        var userId = user.GetId();
        var weekdayToDiscountDict = await loyaltyService.GetDiscountByWeekdayAsync(
            userId: userId,
            venueId: id,
            ct: ct);

        return ApiResult<GetDiscountsByWeekdayResponse>.Build(
            new GetDiscountsByWeekdayResponse(
                Sunday: discountToDto(weekdayToDiscountDict.GetValueOrDefault(DayOfWeek.Sunday)),
                Monday: discountToDto(weekdayToDiscountDict.GetValueOrDefault(DayOfWeek.Monday)),
                Tuesday: discountToDto(weekdayToDiscountDict.GetValueOrDefault(DayOfWeek.Tuesday)),
                Wednesday: discountToDto(weekdayToDiscountDict.GetValueOrDefault(DayOfWeek.Wednesday)),
                Thursday: discountToDto(weekdayToDiscountDict.GetValueOrDefault(DayOfWeek.Thursday)),
                Friday: discountToDto(weekdayToDiscountDict.GetValueOrDefault(DayOfWeek.Friday)),
                Saturday: discountToDto(weekdayToDiscountDict.GetValueOrDefault(DayOfWeek.Saturday))
            )
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
                .Select(p => new PromoDto(Type: p.Type, Body: p.Body))
                .ToArray()
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
                : null
        );
    }
}