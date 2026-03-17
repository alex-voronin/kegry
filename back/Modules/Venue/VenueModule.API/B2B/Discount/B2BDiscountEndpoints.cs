using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using SharedCore.Api;
using SharedCore.Api.Dto;
using SharedCore.Policies;
using VenueModule.API.B2B.Discount.Dto;
using VenueModule.Domain.Aggregates;
using VenueModule.Domain.Exceptions;
using VenueModule.Domain.Repositories;

namespace VenueModule.API.B2B.Discount;

public class B2BDiscountEndpoints : IEndpoints
{
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("")
            .WithTags("Discount & Loyalty Management API")
            .RequireAuthorization(AuthPolicies.B2BAdmin);

        // LoyaltyLevel endpoints
        group.MapGet("/api/v1/venue/{venueId:guid}/loyalty-levels", GetLoyaltyLevels);
        group.MapPut("/api/v1/venue/{venueId:guid}/loyalty-levels", UpsertLoyaltyLevels);

        // Discount endpoints
        group.MapGet("/api/v1/venue/{venueId:guid}/discounts", GetDiscounts);
        group.MapPost("/api/v1/venue/{venueId:guid}/discounts", CreateDiscount);
        group.MapPatch("/api/v1/venue/{venueId:guid}/discounts/{id:guid}", UpdateDiscount);
        group.MapDelete("/api/v1/venue/{venueId:guid}/discounts/{id:guid}", DeleteDiscount);
    }

    // ── LoyaltyLevel endpoints ────────────────────────────────────────────────

    private static async Task<ApiResult<LoyaltyLevelDto[]>> GetLoyaltyLevels(
        [FromRoute] Guid venueId,
        ILoyaltyLevelRepository repository,
        CancellationToken ct)
    {
        var levels = await repository.GetByVenueIdAsync(venueId, ct);
        var dtos = levels
            .Select(l => new LoyaltyLevelDto(l.Id, l.VisitsUpperBound))
            .ToArray();

        return ApiResult<LoyaltyLevelDto[]>.Build(dtos);
    }

    private static async Task<ApiResult.EmptyResult> UpsertLoyaltyLevels(
        [FromRoute] Guid venueId,
        [FromBody] UpsertLoyaltyLevelsRequest request,
        ClaimsPrincipal user,
        IVenueRepository venueRepository,
        ICompanyRepository companyRepository,
        ILoyaltyLevelRepository loyaltyLevelRepository,
        CancellationToken ct)
    {
        var levels = request.LoyaltyLevels
            .Select(dto => new LoyaltyLevel
            {
                Id = dto.Id,
                VenueId = venueId,
                VisitsUpperBound = dto.VisitsUpperBound
            })
            .ToArray();

        LoyaltyLevel.ValidateLevelsCountAndOrder(levels);

        await VenueOwnerGuard.RequireVenueOwnerAsync(venueId, user, venueRepository, companyRepository, ct);
        await loyaltyLevelRepository.DeleteAllForVenueAsync(venueId, ct);

        foreach (var level in levels)
        {
            await loyaltyLevelRepository.AddAsync(level, ct);
        }

        await loyaltyLevelRepository.SaveChangesAsync(ct);

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

    private static async Task<ApiResult.EmptyResult> CreateDiscount(
        [FromRoute] Guid venueId,
        [FromBody] CreateDiscountRequest request,
        ClaimsPrincipal user,
        IVenueRepository venueRepository,
        ICompanyRepository companyRepository,
        IDiscountRepository discountRepository,
        CancellationToken ct)
    {
        await VenueOwnerGuard.RequireVenueOwnerAsync(venueId, user, venueRepository, companyRepository, ct);

        var discountId = Guid.CreateVersion7();
        var discount = new Domain.Aggregates.Discount
        {
            Id = discountId,
            VenueId = venueId,
            LoyaltyLevelId = request.LoyaltyLevelId,
            Percent = request.Percent,
            Days = request.Days
                .Distinct()
                .Select(d => new DiscountDay { DiscountId = discountId, DayOfWeek = d })
                .ToList()
        };

        await discountRepository.AddAsync(discount, ct);
        await discountRepository.SaveChangesAsync(ct);

        return ApiResult.Empty;
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
        await VenueOwnerGuard.RequireVenueOwnerAsync(venueId, user, venueRepository, companyRepository, ct);

        var discount = await discountRepository.GetByIdAsync(id, ct: ct);
        if (discount == null || discount.VenueId != venueId)
        {
            throw VenueExceptions.DiscountNotFound(id);
        }

        discount.LoyaltyLevelId = request.LoyaltyLevelId ?? discount.LoyaltyLevelId;
        discount.Percent = request.Percent ?? discount.Percent;

        if (request.Days != null)
        {
            discount.Days.Clear();
            foreach (var day in request.Days.Distinct())
            {
                discount.Days.Add(new DiscountDay { DiscountId = id, DayOfWeek = day });
            }
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
        await VenueOwnerGuard.RequireVenueOwnerAsync(venueId, user, venueRepository, companyRepository, ct);

        var discount = await discountRepository.GetByIdAsync(id, ct: ct);
        if (discount == null || discount.VenueId != venueId)
        {
            throw VenueExceptions.DiscountNotFound(id);
        }

        discountRepository.Delete(discount);
        await discountRepository.SaveChangesAsync(ct);

        return ApiResult.Empty;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    internal static DiscountDto DomainDiscountToDto(Domain.Aggregates.Discount d) => new(
        Id: d.Id.ToString(),
        VenueId: d.VenueId.ToString(),
        LoyaltyLevelId: d.LoyaltyLevelId,
        Percent: d.Percent,
        Days: d.Days
            .Select(day => day.DayOfWeek)
            .Order()
            .ToArray()
    );
}