using ImTools;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SecurityModule.Infrastructure.Persistence;
using VenueModule.Infrastructure.Persistence;

namespace AdminPanelModule.RazorPages.Pages.Venues;

public class IndexModel : PageModel
{
    private readonly VenueContext _venueContext;
    private readonly SecurityContext _securityContext;

    public IndexModel(VenueContext venueContext, SecurityContext securityContext)
    {
        _venueContext = venueContext;
        _securityContext = securityContext;
    }

    public List<VenueRowDto> Venues { get; set; } = [];

    public async Task OnGetAsync()
    {
        var venues = await _venueContext.Venues
            .IgnoreQueryFilters([NamedFilters.Venue.Verified])
            .Include(v => v.Company)
            .Where(v => !v.IsVerified)
            .OrderBy(v => v.CreatedAt)
            .Take(50)
            .ToListAsync();

        var ownerIds = venues.Select(v => v.Company.OwnerId)
                .Distinct()
                .ToList();
        var ownerIdToEmailDict = _securityContext.Users
            .Where(u => ownerIds.Contains(u.Id))
            .ToDictionary(u => u.Id, u => u.Email!);

        Venues = venues.Select(v =>
                new VenueRowDto
                {
                    Id = v.Id,
                    Address = v.Address,
                    CompanyName = v.Company.Name,
                    OwnerEmail = ownerIdToEmailDict.GetValueOrDefault(v.Company.OwnerId, ""),
                    PhoneNumber = v.PhoneNumber,
                    InstagramUrl = v.InstagramUrl,
                    GoogleMapsUrl = v.GoogleMapsUrl,
                    CreatedAt = v.CreatedAt
                })
            .ToList();
    }

    public async Task<IActionResult> OnPostVerifyAsync(Guid venueId)
    {
        var venue = await _venueContext.Venues
            .IgnoreQueryFilters([NamedFilters.Venue.Verified])
            .FirstOrDefaultAsync(v => v.Id == venueId);
        if (venue == null)
        {
            return NotFound();
        }

        venue.IsVerified = true;
        await _venueContext.SaveChangesAsync();

        TempData["Success"] = "Venue verified successfully.";
        return RedirectToPage();
    }

    public record VenueRowDto
    {
        public Guid Id { get; init; }
        public string Address { get; init; } = "";
        public string CompanyName { get; init; } = "";
        public string OwnerEmail { get; init; } = "";
        public string? PhoneNumber { get; init; }
        public string? InstagramUrl { get; init; }
        public string? GoogleMapsUrl { get; init; }
        public DateTimeOffset CreatedAt { get; init; }
    }
}