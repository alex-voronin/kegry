using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Shared.Infrastructure.Storage;
using SharedCore.Api;
using SharedCore.Api.Dto;
using SharedCore.Extensions;
using VenueModule.API.B2C.Dto;
using VenueModule.Domain.Services;

namespace VenueModule.API.B2C;

public class B2CDiscountEndpoint : IEndpoints
{
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("")
            .WithTags("Discount User API")
            .RequireAuthorization();

        group.MapGet("/api/v1/discounts/available", GetAvailableDiscounts);
    }

    private static async Task<ApiResult<PaginatedResponse<AvailableDiscountDto>>> GetAvailableDiscounts(
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
                CompanyAvatarImageUrl: d.Venue.Company.AvatarImageS3Key != null ? storageService.S3KeyToPublicFileUrl(
                    d.Venue.Company.AvatarImageS3Key, d.Venue.Company.ChangedAt) : null,
                VenueId: d.VenueId.ToString(),
                VenueAddress: d.Venue.Address,
                GoogleMapsUrl: d.Venue.GoogleMapsUrl,
                Percent: d.Percent
            ))
            .ToArray();

        return ApiResult<PaginatedResponse<AvailableDiscountDto>>.Build(
            new PaginatedResponse<AvailableDiscountDto>(dtos, totalCount));
    }
}