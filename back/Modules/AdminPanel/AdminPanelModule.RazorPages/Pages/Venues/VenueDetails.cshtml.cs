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

    public VenueDetailDto? Venue { get; set; }

    [BindProperty]
    public Guid NewQrCode { get; set; }

    public async Task OnGetAsync()
    {
        if (VenueId.HasValue)
        {
            await LoadVenueAsync(VenueId.Value);
        }
    }

    public async Task<IActionResult> OnPostSearchAsync(Guid searchVenueId)
    {
        return RedirectToPage(new { VenueId = searchVenueId });
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
