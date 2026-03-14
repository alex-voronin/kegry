using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using SharedCore.Api;
using SharedCore.Api.Dto;
using VenueModule.API.Dto;
using VenueModule.Domain.Aggregates;
using VenueModule.Domain.Exceptions;
using VenueModule.Domain.Repositories;

namespace VenueModule.API;

public class DiscountManagementEndpoints : IEndpoints
{
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("")
            .WithTags("Discount & Loyalty Management API")
            .RequireAuthorization();

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
        // Build domain objects first — property setters validate Id ∈ {1,2,3} and VisitsUpperBound > 0
        var levels = request.LoyaltyLevels
            .Select(dto => new LoyaltyLevel
            {
                Id = dto.Id,
                VenueId = venueId,
                VisitsUpperBound = dto.VisitsUpperBound
            })
            .ToArray();

        // Collection-level rules: count ≤ 3, strictly ascending VisitsUpperBound by Id
        LoyaltyLevel.ValidateCollection(levels);

        await VenueOwnerGuard.RequireVenueOwnerAsync(venueId, user, venueRepository, companyRepository, ct);
        await loyaltyLevelRepository.DeleteAllForVenueAsync(venueId, ct);

        foreach (var level in levels)
            await loyaltyLevelRepository.AddAsync(level, ct);

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

        var discount = new Discount
        {
            Id = Guid.CreateVersion7(),
            VenueId = venueId,
            LoyaltyLevelId = request.LoyaltyLevelId,
            Percent = request.Percent,
            ActiveBySunday = request.ActiveBySunday,
            ActiveByMonday = request.ActiveByMonday,
            ActiveByTuesday = request.ActiveByTuesday,
            ActiveByWednesday = request.ActiveByWednesday,
            ActiveByThursday = request.ActiveByThursday,
            ActiveByFriday = request.ActiveByFriday,
            ActiveBySaturday = request.ActiveBySaturday
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
            throw VenueExceptions.DiscountNotFound(id);

        discount.LoyaltyLevelId = request.LoyaltyLevelId ?? discount.LoyaltyLevelId;
        discount.Percent = request.Percent ?? discount.Percent;
        discount.ActiveBySunday = request.ActiveBySunday ?? discount.ActiveBySunday;
        discount.ActiveByMonday = request.ActiveByMonday ?? discount.ActiveByMonday;
        discount.ActiveByTuesday = request.ActiveByTuesday ?? discount.ActiveByTuesday;
        discount.ActiveByWednesday = request.ActiveByWednesday ?? discount.ActiveByWednesday;
        discount.ActiveByThursday = request.ActiveByThursday ?? discount.ActiveByThursday;
        discount.ActiveByFriday = request.ActiveByFriday ?? discount.ActiveByFriday;
        discount.ActiveBySaturday = request.ActiveBySaturday ?? discount.ActiveBySaturday;

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
            throw VenueExceptions.DiscountNotFound(id);

        discountRepository.Delete(discount);
        await discountRepository.SaveChangesAsync(ct);

        return ApiResult.Empty;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static DiscountDto DomainDiscountToDto(Discount d) => new(
        Id: d.Id.ToString(),
        VenueId: d.VenueId.ToString(),
        LoyaltyLevelId: d.LoyaltyLevelId,
        Percent: d.Percent,
        ActiveBySunday: d.ActiveBySunday,
        ActiveByMonday: d.ActiveByMonday,
        ActiveByTuesday: d.ActiveByTuesday,
        ActiveByWednesday: d.ActiveByWednesday,
        ActiveByThursday: d.ActiveByThursday,
        ActiveByFriday: d.ActiveByFriday,
        ActiveBySaturday: d.ActiveBySaturday);
}