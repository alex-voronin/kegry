using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using VenueModule.Infrastructure.Persistence;

namespace AdminPanelModule.RazorPages.Pages.Venues;

public class IndexModel : PageModel
{
    private readonly VenueContext _venue;

    public IndexModel(VenueContext venue) => _venue = venue;

    public List<VenueRowDto> Venues { get; set; } = [];

    public async Task OnGetAsync()
    {
        Venues = await _venue.Venues
            .IgnoreQueryFilters([NamedFilters.Venue.Verified])
            .Include(v => v.Company)
            .Where(v => !v.IsVerified)
            .OrderBy(v => v.CreatedAt)
            .Select(v => new VenueRowDto
            {
                Id = v.Id,
                Address = v.Address,
                CompanyName = v.Company.Name,
                CompanyEmail = v.Company.Email,
                PhoneNumber = v.PhoneNumber,
                WebsiteUrl = v.WebsiteUrl,
                GoogleMapsUrl = v.GoogleMapsUrl,
                CreatedAt = v.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostVerifyAsync(Guid venueId)
    {
        var venue = await _venue.Venues.FirstOrDefaultAsync(v => v.Id == venueId);
        if (venue == null)
        {
            return NotFound();
        }

        venue.IsVerified = true;
        await _venue.SaveChangesAsync();

        TempData["Success"] = "Venue verified successfully.";
        return RedirectToPage();
    }

    public record VenueRowDto
    {
        public Guid Id { get; init; }
        public string Address { get; init; } = "";
        public string CompanyName { get; init; } = "";
        public string? CompanyEmail { get; init; }
        public string? PhoneNumber { get; init; }
        public string? WebsiteUrl { get; init; }
        public string? GoogleMapsUrl { get; init; }
        public DateTimeOffset CreatedAt { get; init; }
    }
}
