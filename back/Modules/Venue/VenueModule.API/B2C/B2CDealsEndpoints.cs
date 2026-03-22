using System.Security.Claims;
using ImTools;
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

    private static async Task<ApiResult<PaginatedResponse<AvailableDiscountDto>>> GetDiscounts(
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
                CompanyName: d.Venue.Company.Name,
                CompanyAvatarImageUrl: d.Venue.Company.AvatarImageS3Key != null
                    ? storageService.S3KeyToPublicFileUrl(
                        d.Venue.Company.AvatarImageS3Key, d.Venue.Company.ChangedAt)
                    : null,
                VenueId: d.VenueId.ToString(),
                VenueAddress: d.Venue.Address,
                GoogleMapsUrl: d.Venue.GoogleMapsUrl,
                Percent: d.Percent
            ))
            .ToArray();

        return ApiResult<PaginatedResponse<AvailableDiscountDto>>.Build(
            new PaginatedResponse<AvailableDiscountDto>(dtos, totalCount));
    }

    private static async Task<ApiResult<PaginatedResponse<AvailablePromotionDto>>> GetPromotions(
        [AsParameters] GetAvailableDiscountsRequest request,
        IVenueRepository venueRepository,
        IS3StorageService storageService,
        CancellationToken ct)
    {
        var (venues, totalCount) = await venueRepository.GetWithPromosInRadius(
            latitude: request.Latitude,
            longitude: request.Longitude,
            offset: request.Offset,
            limit: request.Limit,
            ct: ct);

        var dtos = venues
            .Select(v => new AvailablePromotionDto(
                CompanyName: v.Company.Name,
                CompanyAvatarImageUrl: v.Company.AvatarImageS3Key != null
                    ? storageService.S3KeyToPublicFileUrl(
                       v.Company.AvatarImageS3Key, v.Company.ChangedAt)
                    : null,
                VenueId: v.Id.ToString(),
                VenueAddress: v.Address,
                GoogleMapsUrl: v.GoogleMapsUrl,
                Promos: v.Promos
                    .Select(p => new PromoDto(Type: p.Type, Body: p.Body))
                    .ToArray()
            ))
            .ToArray();

        return ApiResult<PaginatedResponse<AvailablePromotionDto>>.Build(
            new PaginatedResponse<AvailablePromotionDto>(dtos, totalCount));
    }
}