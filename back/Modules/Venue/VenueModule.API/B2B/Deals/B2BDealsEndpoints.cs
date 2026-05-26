using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using SharedCore.Api;
using SharedCore.Api.Dto;
using SharedCore.Extensions;
using SharedCore.Policies;
using VenueModule.API.B2B.Deals.Dto;
using VenueModule.Domain.Aggregates;
using VenueModule.Domain.Exceptions;
using VenueModule.Domain.Repositories;
using VenueModule.Infrastructure.Persistence;

namespace VenueModule.API.B2B.Deals;

public class B2BDealsEndpoints : IEndpoints
{
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("")
            .WithTags("Discount & Loyalty Management API")
            .RequireAuthorization(AuthPolicies.B2BAdmin);

        // LoyaltyLevel endpoints
        group.MapGet("/api/v1/b2b/company/{companyId:guid}/loyalty-levels", GetLoyaltyLevels);
        group.MapPut("/api/v1/b2b/company/{companyId:guid}/loyalty-levels", UpdateLoyaltyLevels);

        //Promo endpoints
        group.MapPost("/api/v1/b2b/venue/{venueId:guid}/promotions", CreatePromotion);
        group.MapPut("/api/v1/b2b/venue/{venueId:guid}/promotions/{id:guid}", UpdatePromotion);
        group.MapDelete("/api/v1/b2b/venue/{venueId:guid}/promotions/{id:guid}", DeletePromotion);

        // Discount endpoints
        group.MapGet("/api/v1/b2b/venue/{venueId:guid}/discounts", GetDiscounts);
        group.MapPost("/api/v1/b2b/venue/{venueId:guid}/discounts", CreateDiscount);
        group.MapPut("/api/v1/b2b/venue/{venueId:guid}/discounts/{id:guid}", UpdateDiscount);
        group.MapDelete("/api/v1/b2b/venue/{venueId:guid}/discounts/{id:guid}", DeleteDiscount);
    }

    // ── LoyaltyLevel endpoints ────────────────────────────────────────────────

    private static async Task<ApiResult<LoyaltyLevelDto[]>> GetLoyaltyLevels(
        [FromRoute] Guid companyId,
        ILoyaltyLevelRepository repository,
        CancellationToken ct)
    {
        var levels = await repository.GetByCompanyIdAsync(companyId, ct);
        var dtos = levels
            .Select(l => new LoyaltyLevelDto(l.Id, l.VisitsUpperBound, l.IsActive))
            .ToArray();

        return ApiResult<LoyaltyLevelDto[]>.Build(dtos);
    }

    private static async Task<ApiResult.EmptyResult> UpdateLoyaltyLevels(
        [FromRoute] Guid companyId,
        [FromBody] UpdateLoyaltyLevelsRequest request,
        ClaimsPrincipal user,
        ICompanyRepository companyRepository,
        CancellationToken ct)
    {
        var company = await companyRepository.GetByIdAsync(
            companyId: companyId,
            ownerId: user.GetId(),
            asNoTracking: false,
            includeVenues: false,
            includeLoyaltyLevels: true,
            ignoreVerifiedFilter: true,
            ct: ct);
        if (company == null)
        {
            throw VenueExceptions.CompanyNotFound();
        }

        foreach (var dto in request.LoyaltyLevels)
        {
            var loyaltyLevel = company.LoyaltyLevels.FirstOrDefault(l => l.Id == dto.Id);
            if (loyaltyLevel != null)
            {
                loyaltyLevel.VisitsUpperBound = dto.VisitsUpperBound;
                loyaltyLevel.IsActive = dto.IsActive;
            }
        }

        LoyaltyLevel.ValidateLevelsCountAndOrder(company.LoyaltyLevels);
        await companyRepository.SaveChangesAsync(ct);

        return ApiResult.Empty;
    }

    // ── Discount endpoints ────────────────────────────────────────────────────

    private static async Task<ApiResult<CreatePromotionResponse>> CreatePromotion(
        [FromRoute] Guid venueId,
        [FromBody] CreatePromotionRequest request,
        ClaimsPrincipal user,
        IVenueRepository venueRepository,
        ICompanyRepository companyRepository,
        CancellationToken ct)
    {
        var venue = await VenueOwnerGuard.RequireVenueOwnerAsync(
            venueId: venueId,
            user: user,
            venueRepository: venueRepository,
            companyRepository: companyRepository,
            asNoTracking: false,
            includes: [v => v.Promos],
            ct: ct);

        var promo = new Promo()
        {
            Id = Guid.CreateVersion7(),
            VenueId = venueId,
            Type = request.Type,
            Body = request.Body
        };

        venue.AddPromo(promo);
        await venueRepository.SaveChangesAsync(ct);

        return ApiResult<CreatePromotionResponse>.Build(
            new CreatePromotionResponse(promo.Id)
        );
    }

    private static async Task<ApiResult.EmptyResult> UpdatePromotion(
        [FromRoute] Guid venueId,
        [FromRoute] Guid id,
        [FromBody] UpdatePromotionRequest request,
        ClaimsPrincipal user,
        IVenueRepository venueRepository,
        ICompanyRepository companyRepository,
        IPromoRepository promoRepository,
        CancellationToken ct)
    {
        await VenueOwnerGuard.RequireVenueOwnerAsync(
            venueId: venueId,
            user: user,
            venueRepository: venueRepository,
            companyRepository: companyRepository,
            asNoTracking: true,
            ct: ct);

        var promo = await promoRepository.GetByIdAsync(id,
            ignoreFilters: [NamedFilters.Venue.Verified],
            ct: ct);
        if (promo == null || promo.VenueId != venueId)
        {
            throw VenueExceptions.PromoNotFound(id);
        }

        promo.Type = request.Type;
        promo.Body = request.Body;

        await promoRepository.SaveChangesAsync(ct);

        return ApiResult.Empty;
    }

    private static async Task<ApiResult.EmptyResult> DeletePromotion(
        [FromRoute] Guid venueId,
        [FromRoute] Guid id,
        ClaimsPrincipal user,
        IVenueRepository venueRepository,
        ICompanyRepository companyRepository,
        IPromoRepository promoRepository,
        CancellationToken ct)
    {
        await VenueOwnerGuard.RequireVenueOwnerAsync(
            venueId: venueId,
            user: user,
            venueRepository: venueRepository,
            companyRepository: companyRepository,
            asNoTracking: true,
            ct: ct);

        var promo = await promoRepository.GetByIdAsync(id,
            ignoreFilters: [NamedFilters.Venue.Verified],
            ct: ct);
        if (promo == null || promo.VenueId != venueId)
        {
            throw VenueExceptions.PromoNotFound(id);
        }

        promoRepository.Delete(promo);
        await promoRepository.SaveChangesAsync(ct);

        return ApiResult.Empty;
    }

    // ── Discount endpoints ────────────────────────────────────────────────────

    private static async Task<ApiResult<DiscountDto[]>> GetDiscounts(
        [FromRoute] Guid venueId,
        IDiscountRepository repository,
        CancellationToken ct)
    {
        var discounts = await repository.GetByVenueIdAsync(venueId, ct);
        return ApiResult<DiscountDto[]>.Build(discounts.Select(DomainDiscountToDto).ToArray());
    }

    private static async Task<ApiResult<CreateDiscountResponse>> CreateDiscount(
        [FromRoute] Guid venueId,
        [FromBody] CreateDiscountRequest request,
        ClaimsPrincipal user,
        IVenueRepository venueRepository,
        ICompanyRepository companyRepository,
        CancellationToken ct)
    {
        var venue = await VenueOwnerGuard.RequireVenueOwnerAsync(
            venueId: venueId,
            user: user,
            venueRepository: venueRepository,
            companyRepository: companyRepository,
            asNoTracking: false,
            includes: [v => v.Discounts],
            ct: ct);

        var discountId = Guid.CreateVersion7();
        var discount = new Discount
        {
            Id = discountId,
            VenueId = venueId,
            CompanyId = venue.CompanyId,
            LoyaltyLevelId = request.LoyaltyLevelId,
            Percent = request.Percent,
            Days = request.Days
                .Distinct()
                .Select(d => new DiscountDay { DiscountId = discountId, DayOfWeek = d })
                .ToList()
        };

        venue.AddDiscount(discount);
        await venueRepository.SaveChangesAsync(ct);

        return ApiResult<CreateDiscountResponse>.Build(
            new CreateDiscountResponse(discount.Id)
        );
    }

    private static async Task<ApiResult.EmptyResult> UpdateDiscount(
        [FromRoute] Guid venueId,
        [FromRoute] Guid id,
        [FromBody] UpdateDiscountRequest request,
        ClaimsPrincipal user,
        IVenueRepository venueRepository,
        ICompanyRepository companyRepository,
        IDiscountRepository discountRepository,
        CancellationToken ct)
    {
        await VenueOwnerGuard.RequireVenueOwnerAsync(
            venueId: venueId,
            user: user,
            venueRepository: venueRepository,
            companyRepository: companyRepository,
            asNoTracking: true,
            ct: ct);

        var discount = await discountRepository.GetByIdAsync(id,
            ignoreFilters: [NamedFilters.Venue.Verified],
            includes: [d => d.Days],
            ct: ct);
        if (discount == null || discount.VenueId != venueId)
        {
            throw VenueExceptions.DiscountNotFound(id);
        }

        discount.LoyaltyLevelId = request.LoyaltyLevelId ?? discount.LoyaltyLevelId;
        discount.Percent = request.Percent;

        if (request.Days != null)
        {
            discount.Days = request.Days
                .Distinct()
                .Select(d => new DiscountDay { DiscountId = id, DayOfWeek = d })
                .ToArray();
        }

        await discountRepository.SaveChangesAsync(ct);

        return ApiResult.Empty;
    }

    private static async Task<ApiResult.EmptyResult> DeleteDiscount(
        [FromRoute] Guid venueId,
        [FromRoute] Guid id,
        ClaimsPrincipal user,
        IVenueRepository venueRepository,
        ICompanyRepository companyRepository,
        IDiscountRepository discountRepository,
        CancellationToken ct)
    {
        await VenueOwnerGuard.RequireVenueOwnerAsync(
            venueId: venueId,
            user: user,
            venueRepository: venueRepository,
            companyRepository: companyRepository,
            asNoTracking: true,
            ct: ct);

        var discount = await discountRepository.GetByIdAsync(id,
            ignoreFilters: [NamedFilters.Venue.Verified],
            ct: ct);
        if (discount == null || discount.VenueId != venueId)
        {
            throw VenueExceptions.DiscountNotFound(id);
        }

        discountRepository.Delete(discount);
        await discountRepository.SaveChangesAsync(ct);

        return ApiResult.Empty;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    internal static DiscountDto DomainDiscountToDto(Discount d) => new(
        Id: d.Id.ToString(),
        VenueId: d.VenueId.ToString(),
        LoyaltyLevelId: d.LoyaltyLevelId,
        Percent: d.Percent,
        IsCustom: d.IsCustom,
        Days: d.Days
            .Select(day => day.DayOfWeek)
            .OrderBy(day => day == DayOfWeek.Sunday ? 7 : (int)day)
            .ToArray()
    );
}