using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Shared.Infrastructure.Storage;
using SharedCore.Api;
using SharedCore.Api.Dto;
using SharedCore.Extensions;
using VenueModule.API.B2C.Dto;
using VenueModule.Domain.Repositories;
using VenueModule.Domain.Services;

namespace VenueModule.API.B2C;

public class B2CDealsEndpoints : IEndpoints
{
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("")
            .WithTags("Discount User API")
            .RequireAuthorization();

        group.MapGet("/api/v1/discounts", GetDiscounts);
        group.MapGet("/api/v1/promotions", GetPromotions);
    }

    private static async Task<PaginatedResponse<AvailableDiscountDto>> GetDiscounts(
        [AsParameters] GetAvailableDiscountsRequest request,
        ClaimsPrincipal user,
        ILoyaltyService loyaltyService,
        IS3StorageService storageService,
        CancellationToken ct)
    {
        var userId = user.GetId();
        var (discounts, totalCount) = await loyaltyService.GetAvailableDiscountsAsync(
            userId: userId,
            latitude: request.Latitude,
            longitude: request.Longitude,
            offset: request.Offset,
            limit: request.Limit,
            ct: ct);

        var dtos = discounts
            .Select(d => new AvailableDiscountDto(
                CompanyName: d.Discount.Venue.Company.Name,
                CompanyAvatarImageUrl: d.Discount.Venue.Company.AvatarImageS3Key != null
                    ? storageService.S3KeyToPublicFileUrl(
                        d.Discount.Venue.Company.AvatarImageS3Key, d.Discount.Venue.Company.UpdatedAt)
                    : null,
                VenueId: d.Discount.VenueId,
                VenueAddress: d.Discount.Venue.Address,
                GoogleMapsUrl: d.Discount.Venue.GoogleMapsUrl,
                Percent: d.Discount.Percent,
                IsCustom: d.Discount.IsCustom,
                DistanceMeters: (int)d.DistanceMeters
            ))
            .ToArray();

        return new PaginatedResponse<AvailableDiscountDto>(dtos, totalCount);
    }

    private static async Task<PaginatedResponse<AvailablePromotionDto>> GetPromotions(
        [AsParameters] GetAvailableDiscountsRequest request,
        IVenueRepository venueRepository,
        IS3StorageService storageService,
        CancellationToken ct)
    {
        var (venues, totalCount) = await venueRepository.GetVenuesWithPromosInRadiusAsync(
            latitude: request.Latitude,
            longitude: request.Longitude,
            offset: request.Offset,
            limit: request.Limit,
            ct: ct);

        var dtos = venues
            .Select(v => new AvailablePromotionDto(
                CompanyName: v.Venue.Company.Name,
                CompanyAvatarImageUrl: v.Venue.Company.AvatarImageS3Key != null
                    ? storageService.S3KeyToPublicFileUrl(
                       v.Venue.Company.AvatarImageS3Key, v.Venue.Company.UpdatedAt)
                    : null,
                VenueId: v.Venue.Id.ToString(),
                VenueAddress: v.Venue.Address,
                GoogleMapsUrl: v.Venue.GoogleMapsUrl,
                Promos: v.Venue.Promos
                    .Select(p => new PromoDto(Type: p.Type, Body: p.Body))
                    .ToArray(),
                DistanceMeters: (int)v.DistanceMeters
            ))
            .ToArray();

        return new PaginatedResponse<AvailablePromotionDto>(dtos, totalCount);
    }
}