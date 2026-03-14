using System.Security.Claims;
using VenueModule.Domain.Aggregates;
using VenueModule.Domain.Exceptions;
using VenueModule.Domain.Repositories;
using SharedCore.Extensions;

namespace VenueModule.API;

internal static class VenueOwnerGuard
{
    /// <summary>
    /// Loads the venue and verifies that the authenticated user owns its company.
    /// Throws the appropriate HTTP exception if not found / not the owner.
    /// </summary>
    public static async Task<Venue> RequireVenueOwnerAsync(
        Guid venueId,
        ClaimsPrincipal user,
        IVenueRepository venueRepository,
        ICompanyRepository companyRepository,
        CancellationToken ct)
    {
        var venue = await venueRepository.GetByIdAsync(venueId, ct: ct);
        if (venue == null)
            throw VenueExceptions.VenueNotFound(venueId);

        var company = await companyRepository.GetByIdAsync(venue.CompanyId, ct: ct);
        if (company == null)
            throw VenueExceptions.CompanyNotFound();

        if (company.OwnerId != user.GetId())
            throw VenueExceptions.SomeoneElseCompany(company.Id);

        return venue;
    }
}
