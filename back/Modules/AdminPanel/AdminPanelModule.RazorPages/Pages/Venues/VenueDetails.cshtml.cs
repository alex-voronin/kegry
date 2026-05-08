using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using VenueModule.Infrastructure.Persistence;

namespace AdminPanelModule.RazorPages.Pages.Venues;

public class VenueDetailsModel : PageModel
{
    private readonly VenueContext _venueContext;

    public VenueDetailsModel(VenueContext venueContext)
    {
        _venueContext = venueContext;
    }

    [BindProperty(SupportsGet = true)]
    public Guid? VenueId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? SearchName { get; set; }

    public VenueDetailDto? Venue { get; set; }

    public List<VenueDetailDto> NameSearchResults { get; set; } = [];

    [BindProperty]
    public Guid NewQrCode { get; set; }

    public async Task OnGetAsync()
    {
        if (VenueId.HasValue)
        {
            await LoadVenueAsync(VenueId.Value);
        }

        if (!string.IsNullOrWhiteSpace(SearchName))
        {
            await SearchByNameAsync(SearchName);
        }
    }

    public async Task<IActionResult> OnPostSearchAsync(Guid searchVenueId)
    {
        return RedirectToPage(new { VenueId = searchVenueId });
    }

    public async Task<IActionResult> OnPostSearchByNameAsync(string searchName)
    {
        return RedirectToPage(new { SearchName = searchName });
    }

    public async Task<IActionResult> OnPostUpdateQrAsync(Guid venueId)
    {
        var venue = await _venueContext.Venues
            .IgnoreQueryFilters([NamedFilters.Venue.Verified])
            .FirstOrDefaultAsync(v => v.Id == venueId);

        if (venue == null)
        {
            return NotFound();
        }

        venue.QrCode = NewQrCode;
        await _venueContext.SaveChangesAsync();

        TempData["Success"] = "QR Code updated successfully.";
        return RedirectToPage(new { VenueId = venueId });
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
        return RedirectToPage(new { VenueId = venueId });
    }

    public async Task<IActionResult> OnPostUnverifyAsync(Guid venueId)
    {
        var venue = await _venueContext.Venues
            .IgnoreQueryFilters([NamedFilters.Venue.Verified])
            .FirstOrDefaultAsync(v => v.Id == venueId);

        if (venue == null)
        {
            return NotFound();
        }

        venue.IsVerified = false;
        await _venueContext.SaveChangesAsync();

        TempData["Success"] = "Venue unverified successfully.";
        return RedirectToPage(new { VenueId = venueId });
    }

    private async Task LoadVenueAsync(Guid id)
    {
        var venue = await _venueContext.Venues
            .IgnoreQueryFilters([NamedFilters.Venue.Verified])
            .Include(v => v.Company)
            .FirstOrDefaultAsync(v => v.Id == id);

        if (venue != null)
        {
            Venue = new VenueDetailDto
            {
                Id = venue.Id,
                Address = venue.Address,
                CompanyName = venue.Company.Name,
                QrCode = venue.QrCode,
                InstagramUrl = venue.InstagramUrl,
                GoogleMapsUrl = venue.GoogleMapsUrl,
                IsVerified = venue.IsVerified,
                CreatedAt = venue.CreatedAt
            };
            NewQrCode = venue.QrCode;
        }
    }

    private async Task SearchByNameAsync(string name)
    {
        var lowerName = name.ToLower();

        var venues = await _venueContext.Venues
            .IgnoreQueryFilters([NamedFilters.Venue.Verified])
            .Include(v => v.Company)
            .Where(v => v.Company.Name.ToLower().Contains(lowerName))
            .OrderBy(v => v.Address)
            .Take(20)
            .ToListAsync();

        NameSearchResults = venues.Select(v => new VenueDetailDto
        {
            Id = v.Id,
            Address = v.Address,
            CompanyName = v.Company.Name,
            QrCode = v.QrCode,
            InstagramUrl = v.InstagramUrl,
            GoogleMapsUrl = v.GoogleMapsUrl,
            IsVerified = v.IsVerified,
            CreatedAt = v.CreatedAt
        }).ToList();
    }

    public record VenueDetailDto
    {
        public Guid Id { get; init; }
        public string Address { get; init; } = "";
        public string CompanyName { get; init; } = "";
        public Guid QrCode { get; init; }
        public string? InstagramUrl { get; init; }
        public string? GoogleMapsUrl { get; init; }
        public bool IsVerified { get; init; }
        public DateTimeOffset CreatedAt { get; init; }
    }
}
