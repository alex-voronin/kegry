using FeedModule.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SecurityModule.Infrastructure.Persistence;
using VenueModule.Infrastructure.Persistence;

namespace AdminPanelModule.RazorPages.Pages;

public class IndexModel : PageModel
{
    private readonly SecurityContext _security;
    private readonly VenueContext _venue;
    private readonly FeedContext _feed;

    public IndexModel(SecurityContext security, VenueContext venue, FeedContext feed)
    {
        _security = security;
        _venue = venue;
        _feed = feed;
    }

    public int PendingVenues { get; set; }
    public int TotalB2BUsers { get; set; }
    public int OpenReports { get; set; }

    public async Task OnGetAsync()
    {
        PendingVenues = await _venue.Venues
            .IgnoreQueryFilters()
            .CountAsync(v => !v.IsVerified && !v.IsDeleted);

        TotalB2BUsers = await _security.Users
            .CountAsync(u => u.Type == SecurityModule.Domain.ValueObjects.UserType.B2B);

        OpenReports = await _feed.FeedUserRelations
            .CountAsync(r => r.ReportMessage != null && r.ReportMessage != "");
    }
}
