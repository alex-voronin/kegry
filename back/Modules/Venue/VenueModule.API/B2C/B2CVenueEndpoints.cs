using Shared.Infrastructure.Storage;
using SharedCore.Exceptions;
using VenueModule.API.B2C.Dto;
using VenueModule.API.Shared;
using VenueModule.Domain.ValueObjects;
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
        usersApiGroup.MapGet("/api/v1/venues", GetVenues);
        usersApiGroup.MapGet("/api/v1/venues/map", GetVenuesMap);
        usersApiGroup.MapGet("/api/v1/venue/{id:guid}", GetVenue);
        usersApiGroup.MapGet("/api/v1/venue/{id:guid}/discounts", GetDiscountsByWeekday);
        usersApiGroup.MapPost("/api/v1/venue/{qrCode:guid}/qr-check-in", CheckInToVenue);
        usersApiGroup.MapGet("/api/v1/venue/{qrCode:guid}/qr-check-in/settings", GetCheckInSettings);
    }

    private static async Task<ApiResult<CheckInResponse>> CheckInToVenue(
        [FromRoute] Guid qrCode,
        [FromBody] CheckInRequest request,
        ClaimsPrincipal user,
        ILoyaltyService loyaltyService,
        CancellationToken ct)
    {
        var userId = user.GetId();
        var (venueId, error) = await loyaltyService.CheckInToVenue(
            userId: userId,
            qrCode: qrCode,
            userLatitude: request.Latitude,
            userLongitude: request.Longitude,
            pinCode: request.PinCode,
            ct: ct);

        return ApiResult<CheckInResponse>.Build(
            result: new CheckInResponse(venueId),
            errors: error != null
                ?
                [
                    new ApiError
                    {
                        Error = error,
                        Code = nameof(ErrorCode.ValidationError)
                    }
                ]
                : null
        );
    }

    private static async Task<ApiResult<GetCheckInSettingsResponse>> GetCheckInSettings(
        [FromRoute] Guid qrCode,
        IVenueRepository venueRepository,
        CancellationToken ct)
    {
        var venue = await venueRepository.GetByQrCodeAsync(qrCode, ct);
        if (venue == null)
        {
            throw VenueExceptions.VenueNotFoundByQr(qrCode);
        }

        return ApiResult<GetCheckInSettingsResponse>.Build(
            result: new GetCheckInSettingsResponse(venue.IsPinCheckInAllowed)
        );
    }

    private static async Task<ApiResult<PaginatedResponse<VenueListItemDto>>> GetVenues(
        [AsParameters] GetVenuesRequest request,
        IVenueRepository repository,
        ITagRepository tagRepository,
        IS3StorageService storageService,
        CancellationToken ct)
    {
        var (venues, totalCount) = await repository.GetVenuesPaginatedAsync(
            latitude: request.Latitude,
            longitude: request.Longitude,
            offset: request.Offset,
            limit: request.Limit,
            tagIds: request.Filter?.TagIds,
            companyName: request.Filter?.CompanyName,
            ct: ct);
        var dtos = venues
            .Select(v =>
                new VenueListItemDto(
                    Id: v.Venue.Id.ToString(),
                    CompanyName: v.Venue.Company.Name,
                    CompanyAvatarImageUrl: v.Venue.Company.AvatarImageS3Key != null
                        ? storageService
                            .S3KeyToPublicFileUrl(v.Venue.Company.AvatarImageS3Key, v.Venue.Company.UpdatedAt)
                        : null,
                    Address: v.Venue.Address,
                    DistanceMeters: (int)v.DistanceMeters,
                    GoogleMapsUrl: v.Venue.GoogleMapsUrl,
                    TagIds: GetSortedTagIds(v.Venue)
                )
            )
            .ToArray();
        return ApiResult<PaginatedResponse<VenueListItemDto>>.Build(
            new PaginatedResponse<VenueListItemDto>(dtos, totalCount)
        );
    }

    private static async Task<ApiResult<VenueMapItemDto[]>> GetVenuesMap(
        [AsParameters] GetVenuesMapRequest request,
        IVenueRepository repository,
        ITagRepository tagRepository,
        IS3StorageService storageService,
        CancellationToken ct)
    {
        var venues = await repository.GetVenuesInBoundsAsync(
            request.MinLat, request.MaxLat,
            request.MinLng, request.MaxLng,
            limit: 200,
            tagIds: request.Filter?.TagIds,
            companyName: request.Filter?.CompanyName,
            ct: ct);

        var dtos = venues.Select(v => new VenueMapItemDto(
            Id: v.Id.ToString(),
            CompanyName: v.Company.Name,
            CompanyAvatarImageUrl: v.Company.AvatarImageS3Key != null
                ? storageService.S3KeyToPublicFileUrl(v.Company.AvatarImageS3Key, v.Company.UpdatedAt)
                : null,
            Address: v.Address,
            Latitude: v.Coordinates.Y,
            Longitude: v.Coordinates.X,
            TagIds: GetSortedTagIds(v)
        )).ToArray();

        return ApiResult<VenueMapItemDto[]>.Build(dtos);
    }

    private static async Task<ApiResult<GetVenueResponse>> GetVenue(
        [FromRoute] Guid id,
        ClaimsPrincipal user,
        IVenueRepository repository,
        ILoyaltyService loyaltyService,
        IS3StorageService storageService,
        CancellationToken ct)
    {
        var venue = await repository.GetByIdWithAllRelationsAsync(id, ct: ct);
        if (venue == null)
        {
            throw VenueExceptions.VenueNotFound(id);
        }

        var companyDto = DomainCompanyToDto(storageService, venue.Company);
        var venueDto = DomainVenueToDto(storageService, venue);
        var loyaltyLevel = await loyaltyService.GetLoyaltyLevelWithDiscountAsync(
            userId: user.GetId(),
            venue: venue,
            ct: ct);

        var loyaltyLevelDto = new LoyaltyLevelWithDiscountDto(
            UserLoyaltyLevel: loyaltyLevel.UserLoyaltyLevel,
            VisitsCount: loyaltyLevel.VisitsCount,
            VisitsLowerBound: loyaltyLevel.VisitsLowerBound,
            VisitsUpperBound: loyaltyLevel.VisitsUpperBound,
            Discount: loyaltyLevel.Discount,
            NextLevelDiscount: loyaltyLevel.NextLevelDiscount,
            IsCustomDiscount: loyaltyLevel.Discount == null
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
                CurrentLevelDiscount: domainDiscount?.Discount,
                NextLevelDiscount: domainDiscount?.NextLevelDiscount,
                IsCustomDiscount: domainDiscount?.IsCustomDiscount ?? false
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

    private static string[] GetSortedTagIds(Venue venue)
    {
        return venue.VenueTagLinks
            .OrderBy(vt => vt.Tag.Category switch
            {
                TagCategory.PriceLevel => 0,
                TagCategory.HospitalityAndRules => 1,
                TagCategory.SmokingPolicy => 2,
                TagCategory.FoodAndKitchen => 3,
                TagCategory.InfrastructureAndAmenities => 4,
                TagCategory.DrinkCategory => 5,
                _ => 99
            })
            .ThenBy(vt => vt.Tag.SortOrder)
            .Select(vt => vt.Tag.Id.ToString())
            .ToArray();
    }

    private static VenueDto DomainVenueToDto(IS3StorageService storageService, Venue venue)
    {
        return new VenueDto(
            Id: venue.Id.ToString(),
            QrCode: venue.QrCode.ToString(),
            IsPinCheckInAllowed: venue.IsPinCheckInAllowed,
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
            Tags: venue.VenueTagLinks.Select(vt =>
                    new TagDto(Id: vt.Tag.Id, Value: vt.Tag.Value, ParentId: vt.Tag.ParentId)
                )
                .ToArray(),
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
                ? storageService.S3KeyToPublicFileUrl(company.AvatarImageS3Key, company.UpdatedAt)
                : null
        );
    }
}